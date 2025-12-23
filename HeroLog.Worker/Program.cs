using HeroLog.Shared.Configuration;
using HeroLog.Worker.Data;
using HeroLog.Worker.Repositories;
using HeroLog.Worker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Configure RabbitMQ settings
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

// Configure MongoDB settings
builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
    options.DatabaseName = builder.Configuration.GetSection("MongoDB")["DatabaseName"] ?? "HeroLogDb";
    options.CollectionName = builder.Configuration.GetSection("MongoDB")["CollectionName"] ?? "ServiceLogs";
});

// Configure SQL Server DbContext
builder.Services.AddDbContext<LogDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

// Register MongoDB repository
builder.Services.AddSingleton<IMongoLogRepository, MongoLogRepository>();

// Register the background service
builder.Services.AddHostedService<LogConsumerService>();

var host = builder.Build();

// Apply migrations on startup
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<LogDbContext>();
        context.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");
    }
}

host.Run();
