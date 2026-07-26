using HeroLog.Shared.Models;

namespace HeroLog.Worker.Repositories;

/// <summary>
/// Interface for MongoDB log repository
/// </summary>
public interface IMongoLogRepository
{
    /// <summary>
    /// Saves a log document to MongoDB
    /// </summary>
    /// <param name="log">The log document to save</param>
    Task SaveLogAsync(ServiceLogDocument log);
}
