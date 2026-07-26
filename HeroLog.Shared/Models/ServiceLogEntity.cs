using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HeroLog.Shared.Models;

/// <summary>
/// Entity Framework Core entity for ServiceLog
/// </summary>
[Table("ServiceLogs")]
public class ServiceLogEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LogLevel { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    [MaxLength(200)]
    public string Source { get; set; } = string.Empty;

    public string? AdditionalData { get; set; }

    /// <summary>
    /// Converts from ServiceLog DTO to Entity
    /// </summary>
    public static ServiceLogEntity FromServiceLog(ServiceLog log)
    {
        return new ServiceLogEntity
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
