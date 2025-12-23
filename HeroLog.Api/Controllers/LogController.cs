using HeroLog.Api.Services;
using HeroLog.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace HeroLog.Api.Controllers;

/// <summary>
/// Controller for handling log operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LogController : ControllerBase
{
    private readonly IRabbitMqProducer _rabbitMqProducer;
    private readonly ILogger<LogController> _logger;

    public LogController(IRabbitMqProducer rabbitMqProducer, ILogger<LogController> logger)
    {
        _rabbitMqProducer = rabbitMqProducer;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a log message to RabbitMQ
    /// </summary>
    /// <param name="log">The service log to publish</param>
    /// <returns>Action result indicating success or failure</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult PublishLog([FromBody] ServiceLog log)
    {
        try
        {
            if (log == null)
            {
                _logger.LogWarning("Received null log object");
                return BadRequest("Log object cannot be null");
            }

            if (string.IsNullOrWhiteSpace(log.ServiceName))
            {
                _logger.LogWarning("Received log with empty ServiceName");
                return BadRequest("ServiceName is required");
            }

            if (string.IsNullOrWhiteSpace(log.Message))
            {
                _logger.LogWarning("Received log with empty Message");
                return BadRequest("Message is required");
            }

            // Ensure Id and Timestamp are set
            if (log.Id == Guid.Empty)
            {
                log.Id = Guid.NewGuid();
            }

            if (log.Timestamp == DateTime.MinValue)
            {
                log.Timestamp = DateTime.UtcNow;
            }

            _rabbitMqProducer.PublishLog(log);

            _logger.LogInformation("Successfully published log {LogId} from service {ServiceName}", log.Id, log.ServiceName);

            return Ok(new { message = "Log published successfully", logId = log.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing log");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while publishing the log");
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}
