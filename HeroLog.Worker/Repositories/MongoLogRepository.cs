using HeroLog.Shared.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HeroLog.Worker.Repositories;

/// <summary>
/// MongoDB configuration settings
/// </summary>
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "HeroLogDb";
    public string CollectionName { get; set; } = "ServiceLogs";
}

/// <summary>
/// MongoDB repository for saving service logs
/// </summary>
public class MongoLogRepository : IMongoLogRepository
{
    private readonly IMongoCollection<ServiceLogDocument> _collection;
    private readonly ILogger<MongoLogRepository> _logger;

    public MongoLogRepository(IOptions<MongoDbSettings> settings, ILogger<MongoLogRepository> logger)
    {
        _logger = logger;

        try
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<ServiceLogDocument>(settings.Value.CollectionName);

            _logger.LogInformation("MongoDB connection established to {DatabaseName}", settings.Value.DatabaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish MongoDB connection");
            throw;
        }
    }

    /// <summary>
    /// Saves a log document to MongoDB
    /// </summary>
    public async Task SaveLogAsync(ServiceLogDocument log)
    {
        try
        {
            await _collection.InsertOneAsync(log);
            _logger.LogInformation("Successfully saved log {LogId} to MongoDB", log.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save log {LogId} to MongoDB", log.Id);
            throw;
        }
    }
}
