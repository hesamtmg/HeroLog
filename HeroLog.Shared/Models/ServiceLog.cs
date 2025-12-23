namespace HeroLog.Shared.Models;

/// <summary>
/// Represents a service log entry
/// </summary>
public class ServiceLog
{
    /// <summary>
    /// Unique identifier for the log entry
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the service that generated the log
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Log level (Info, Warning, Error, etc.)
    /// </summary>
    public string LogLevel { get; set; } = string.Empty;

    /// <summary>
    /// Log message content
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the log was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Source of the log entry
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Additional data as JSON string
    /// </summary>
    public string? AdditionalData { get; set; }
}
