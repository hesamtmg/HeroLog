using System.Text;
using System.Text.Json;
using HeroLog.Shared.Configuration;
using HeroLog.Shared.Models;
using HeroLog.Worker.Data;
using HeroLog.Worker.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HeroLog.Worker.Services;

/// <summary>
/// Background service that consumes log messages from RabbitMQ and saves to both MSSQL and MongoDB
/// </summary>
public class LogConsumerService : BackgroundService
{
    private readonly ILogger<LogConsumerService> _logger;
    private readonly RabbitMqSettings _rabbitMqSettings;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _connection;
    private IChannel? _channel;

    public LogConsumerService(
        ILogger<LogConsumerService> logger,
        IOptions<RabbitMqSettings> rabbitMqSettings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _rabbitMqSettings = rabbitMqSettings.Value;
        _serviceProvider = serviceProvider;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LogConsumerService is starting...");

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqSettings.HostName,
                Port = _rabbitMqSettings.Port,
                UserName = _rabbitMqSettings.UserName,
                Password = _rabbitMqSettings.Password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declare the queue
            await _channel.QueueDeclareAsync(
                queue: _rabbitMqSettings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            // Set QoS to process one message at a time
            await _channel.BasicQosAsync(0, 1, false, cancellationToken);

            _logger.LogInformation("Connected to RabbitMQ at {HostName}:{Port}, listening to queue: {QueueName}",
                _rabbitMqSettings.HostName, _rabbitMqSettings.Port, _rabbitMqSettings.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogError("Channel is not initialized");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var serviceLog = JsonSerializer.Deserialize<ServiceLog>(message);

                if (serviceLog != null)
                {
                    _logger.LogInformation("Received log {LogId} from service {ServiceName}",
                        serviceLog.Id, serviceLog.ServiceName);

                    // Process the log (save to both databases)
                    await ProcessLogAsync(serviceLog);

                    // Acknowledge the message
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);

                    _logger.LogInformation("Successfully processed and acknowledged log {LogId}", serviceLog.Id);
                }
                else
                {
                    _logger.LogWarning("Deserialized log is null, rejecting message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message: {Message}", message);
                // Reject the message without requeue
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message, will retry");
                // Requeue the message for retry
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _rabbitMqSettings.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Started consuming messages from queue: {QueueName}", _rabbitMqSettings.QueueName);

        // Keep the service running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer service is shutting down gracefully");
        }
    }

    /// <summary>
    /// Processes a service log by saving it to both MSSQL and MongoDB
    /// </summary>
    private async Task ProcessLogAsync(ServiceLog serviceLog)
    {
        using var scope = _serviceProvider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var mongoRepository = scope.ServiceProvider.GetRequiredService<IMongoLogRepository>();

        var sqlSucceeded = false;
        var mongoSucceeded = false;

        // Save to MSSQL
        try
        {
            var entity = ServiceLogEntity.FromServiceLog(serviceLog);
            await dbContext.ServiceLogs.AddAsync(entity);
            await dbContext.SaveChangesAsync();
            sqlSucceeded = true;
            _logger.LogInformation("Saved log {LogId} to MSSQL", serviceLog.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save log {LogId} to MSSQL", serviceLog.Id);
        }

        // Save to MongoDB
        try
        {
            var document = ServiceLogDocument.FromServiceLog(serviceLog);
            await mongoRepository.SaveLogAsync(document);
            mongoSucceeded = true;
            _logger.LogInformation("Saved log {LogId} to MongoDB", serviceLog.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save log {LogId} to MongoDB", serviceLog.Id);
        }

        // Log the final status
        if (sqlSucceeded && mongoSucceeded)
        {
            _logger.LogInformation("Successfully saved log {LogId} to both databases", serviceLog.Id);
        }
        else if (sqlSucceeded || mongoSucceeded)
        {
            _logger.LogWarning("Partially saved log {LogId}. MSSQL: {SqlStatus}, MongoDB: {MongoStatus}",
                serviceLog.Id, sqlSucceeded ? "Success" : "Failed", mongoSucceeded ? "Success" : "Failed");
        }
        else
        {
            _logger.LogError("Failed to save log {LogId} to both databases", serviceLog.Id);
            throw new InvalidOperationException($"Failed to persist log {serviceLog.Id} to any database");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LogConsumerService is stopping...");

        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
