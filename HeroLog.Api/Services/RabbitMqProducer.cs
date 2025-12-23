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
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private bool _disposed = false;

    public RabbitMqProducer(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqProducer> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare the queue
            _channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null).GetAwaiter().GetResult();

            _logger.LogInformation("RabbitMQ connection established to {HostName}:{Port}", _settings.HostName, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            throw;
        }
    }

    /// <summary>
    /// Publishes a service log to RabbitMQ queue
    /// </summary>
    public void PublishLog(ServiceLog log)
    {
        try
        {
            var message = JsonSerializer.Serialize(log);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _settings.QueueName,
                mandatory: false,
                basicProperties: properties,
                body: body).GetAwaiter().GetResult();

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
            _logger.LogInformation("RabbitMQ connection disposed");
        }

        _disposed = true;
    }
}
