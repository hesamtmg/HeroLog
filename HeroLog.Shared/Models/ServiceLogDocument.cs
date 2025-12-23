using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HeroLog.Shared.Models;

/// <summary>
/// MongoDB document for ServiceLog
/// </summary>
public class ServiceLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("serviceName")]
    [BsonRequired]
    public string ServiceName { get; set; } = string.Empty;

    [BsonElement("logLevel")]
    [BsonRequired]
    public string LogLevel { get; set; } = string.Empty;

    [BsonElement("message")]
    [BsonRequired]
    public string Message { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    [BsonRequired]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Timestamp { get; set; }

    [BsonElement("source")]
    [BsonRequired]
    public string Source { get; set; } = string.Empty;

    [BsonElement("additionalData")]
    [BsonIgnoreIfNull]
    public string? AdditionalData { get; set; }

    /// <summary>
    /// Converts from ServiceLog DTO to Document
    /// </summary>
    public static ServiceLogDocument FromServiceLog(ServiceLog log)
    {
        return new ServiceLogDocument
        {
            Id = log.Id,
            ServiceName = log.ServiceName,
            LogLevel = log.LogLevel,
            Message = log.Message,
            Timestamp = log.Timestamp,
            Source = log.Source,
            AdditionalData = log.AdditionalData
        };
    }
}
