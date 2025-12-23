namespace HeroLog.Shared.Configuration;

/// <summary>
/// RabbitMQ configuration settings
/// </summary>
public class RabbitMqSettings
{
    /// <summary>
    /// RabbitMQ host name
    /// </summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// RabbitMQ username
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// RabbitMQ password
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Queue name for service logs
    /// </summary>
    public string QueueName { get; set; } = "service-logs";
}
