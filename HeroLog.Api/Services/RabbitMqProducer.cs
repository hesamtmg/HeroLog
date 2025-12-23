using System.Text;
using System.Text.Json;
using HeroLog.Shared.Configuration;
using HeroLog.Shared.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HeroLog.Api.Services;

/// <summary>
/// RabbitMQ producer service for publishing log messages
/// </summary>
public class RabbitMqProducer : IRabbitMqProducer, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqProducer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);
    private bool _initialized = false;
    private bool _disposed = false;

    public RabbitMqProducer(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqProducer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
            return;

        await _initializationSemaphore.WaitAsync();
        try
        {
            if (_initialized)
                return;

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Declare the queue
            await _channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _initialized = true;
            _logger.LogInformation("RabbitMQ connection established to {HostName}:{Port}", _settings.HostName, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    /// <summary>
    /// Publishes a service log to RabbitMQ queue
    /// </summary>
    public async Task PublishLogAsync(ServiceLog log)
    {
        await EnsureInitializedAsync();

        if (_channel == null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized");
        }

        try
        {
            var message = JsonSerializer.Serialize(log);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _settings.QueueName,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published log {LogId} to RabbitMQ queue {QueueName}", log.Id, _settings.QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish log {LogId} to RabbitMQ", log.Id);
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _initializationSemaphore?.Dispose();
            _logger.LogInformation("RabbitMQ connection disposed");
        }

        _disposed = true;
    }
}
