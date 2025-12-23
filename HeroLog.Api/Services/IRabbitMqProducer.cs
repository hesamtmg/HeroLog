using HeroLog.Shared.Models;

namespace HeroLog.Api.Services;

/// <summary>
/// Interface for publishing messages to RabbitMQ
/// </summary>
public interface IRabbitMqProducer
{
    /// <summary>
    /// Publishes a service log to RabbitMQ queue
    /// </summary>
    /// <param name="log">The service log to publish</param>
    Task PublishLogAsync(ServiceLog log);
}
