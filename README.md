# HeroLog - .NET Logging Solution

A comprehensive .NET 8.0 logging solution that uses RabbitMQ for message queuing with dual persistence to both Microsoft SQL Server and MongoDB.

## Architecture Overview

The HeroLog solution consists of three main components:

1. **HeroLog.Api** - ASP.NET Core Web API that receives log messages and publishes them to RabbitMQ
2. **HeroLog.Worker** - Background service that consumes messages from RabbitMQ and persists them to both MSSQL and MongoDB
3. **HeroLog.Shared** - Shared library containing common models, entities, and configurations

```
┌─────────────┐      ┌──────────────┐      ┌─────────────────┐
│             │      │              │      │                 │
│  HeroLog.Api│─────▶│   RabbitMQ   │─────▶│  HeroLog.Worker │
│             │      │              │      │                 │
└─────────────┘      └──────────────┘      └────────┬────────┘
                                                    │
                                            ┌───────┴────────┐
                                            │                │
                                            ▼                ▼
                                       ┌─────────┐    ┌──────────┐
                                       │  MSSQL  │    │ MongoDB  │
                                       └─────────┘    └──────────┘
```

## Features

- ✅ RESTful API for log submission
- ✅ RabbitMQ message queue for reliable message delivery
- ✅ Dual persistence to both SQL Server and MongoDB
- ✅ Graceful error handling and retry logic
- ✅ Health check endpoints
- ✅ Swagger/OpenAPI documentation
- ✅ Automatic database migrations
- ✅ Docker Compose for easy dependency setup
- ✅ CORS support
- ✅ Comprehensive logging with ILogger

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for running dependencies)
- OR manually installed:
  - RabbitMQ 3.13+
  - Microsoft SQL Server 2022+
  - MongoDB 7.0+

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/hesamtmg/HeroLog.git
cd HeroLog
```

### 2. Start Dependencies with Docker Compose

```bash
docker-compose up -d
```

This will start:
- RabbitMQ on ports 5672 (AMQP) and 15672 (Management UI)
- SQL Server on port 1433
- MongoDB on port 27017

**Access RabbitMQ Management UI**: http://localhost:15672 (guest/guest)

### 3. Update Connection Strings (if needed)

If you're not using Docker or need different connection settings, update the `appsettings.json` files:

**HeroLog.Api/appsettings.json**:
```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "QueueName": "service-logs"
  }
}
```

**HeroLog.Worker/appsettings.json**:
```json
{
  "RabbitMQ": { ... },
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=HeroLogDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;",
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "DatabaseName": "HeroLogDb",
    "CollectionName": "ServiceLogs"
  }
}
```

### 4. Build the Solution

```bash
dotnet build
```

### 5. Run Database Migrations

The Worker project will automatically apply migrations on startup, but you can also run them manually:

```bash
cd HeroLog.Worker
dotnet ef database update
```

Or create new migrations:

```bash
dotnet ef migrations add YourMigrationName
```

### 6. Run the Applications

Open two terminal windows:

**Terminal 1 - Start the API**:
```bash
cd HeroLog.Api
dotnet run
```

The API will start at:
- HTTPS: https://localhost:7092
- HTTP: http://localhost:5218
- Swagger UI: https://localhost:7092/swagger

**Terminal 2 - Start the Worker**:
```bash
cd HeroLog.Worker
dotnet run
```

The Worker will start consuming messages from RabbitMQ and persisting them to both databases.

## API Usage

### Publish a Log

**Endpoint**: `POST /api/log`

**Request Body**:
```json
{
  "serviceName": "MyService",
  "logLevel": "Information",
  "message": "This is a test log message",
  "source": "TestController",
  "additionalData": "{\"userId\": 123, \"action\": \"login\"}"
}
```

**cURL Example**:
```bash
curl -X POST https://localhost:7092/api/log \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName": "MyService",
    "logLevel": "Information",
    "message": "User logged in successfully",
    "source": "AuthController",
    "additionalData": "{\"userId\": 456}"
  }'
```

**Response** (200 OK):
```json
{
  "message": "Log published successfully",
  "logId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Health Check

**Endpoint**: `GET /api/log/health`

**cURL Example**:
```bash
curl https://localhost:7092/api/log/health
```

**Response**:
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Testing the Solution

### 1. Test API with Swagger

Navigate to https://localhost:7092/swagger and use the interactive UI to test the endpoints.

### 2. Test with PowerShell

```powershell
$body = @{
    serviceName = "TestService"
    logLevel = "Error"
    message = "Test error message"
    source = "PowerShellScript"
    additionalData = '{"testKey": "testValue"}'
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7092/api/log" `
    -Method Post `
    -Body $body `
    -ContentType "application/json" `
    -SkipCertificateCheck
```

### 3. Verify Data Persistence

**SQL Server**:
```bash
# Connect to SQL Server (using Docker)
docker exec -it herolog-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd

# Query logs
SELECT TOP 10 * FROM HeroLogDb.dbo.ServiceLogs ORDER BY Timestamp DESC;
GO
```

**MongoDB**:
```bash
# Connect to MongoDB (using Docker)
docker exec -it herolog-mongodb mongosh

# Switch to database and query
use HeroLogDb
db.ServiceLogs.find().sort({timestamp: -1}).limit(10).pretty()
```

## Project Structure

```
HeroLog/
├── HeroLog.sln                          # Solution file
├── .gitignore                           # Git ignore file
├── README.md                            # This file
├── docker-compose.yml                   # Docker Compose for dependencies
├── HeroLog.Api/                         # Web API Project
│   ├── Controllers/
│   │   └── LogController.cs             # REST API controller
│   ├── Services/
│   │   ├── IRabbitMqProducer.cs         # Producer interface
│   │   └── RabbitMqProducer.cs          # RabbitMQ producer implementation
│   ├── Program.cs                       # API startup and configuration
│   └── appsettings.json                 # API configuration
├── HeroLog.Worker/                      # Worker Service Project
│   ├── Services/
│   │   └── LogConsumerService.cs        # RabbitMQ consumer background service
│   ├── Data/
│   │   ├── LogDbContext.cs              # EF Core DbContext
│   │   └── Migrations/                  # EF Core migrations
│   ├── Repositories/
│   │   ├── IMongoLogRepository.cs       # MongoDB repository interface
│   │   └── MongoLogRepository.cs        # MongoDB repository implementation
│   ├── Program.cs                       # Worker startup and configuration
│   └── appsettings.json                 # Worker configuration
└── HeroLog.Shared/                      # Shared Library
    ├── Models/
    │   ├── ServiceLog.cs                # DTO model
    │   ├── ServiceLogEntity.cs          # EF Core entity
    │   └── ServiceLogDocument.cs        # MongoDB document
    └── Configuration/
        └── RabbitMqSettings.cs          # RabbitMQ configuration class
```

## Configuration

### RabbitMQ Settings

Both API and Worker projects use the same RabbitMQ configuration:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "QueueName": "service-logs"
  }
}
```

### Database Connection Strings

**SQL Server** (Worker only):
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=HeroLogDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  }
}
```

**MongoDB** (Worker only):
```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017"
  },
  "MongoDB": {
    "DatabaseName": "HeroLogDb",
    "CollectionName": "ServiceLogs"
  }
}
```

## Troubleshooting

### RabbitMQ Connection Errors

- Ensure RabbitMQ is running: `docker ps | grep rabbitmq`
- Check RabbitMQ logs: `docker logs herolog-rabbitmq`
- Verify connection settings in appsettings.json

### SQL Server Connection Errors

- Ensure SQL Server is running: `docker ps | grep sqlserver`
- Check SQL Server logs: `docker logs herolog-sqlserver`
- Update the connection string with correct credentials

### MongoDB Connection Errors

- Ensure MongoDB is running: `docker ps | grep mongodb`
- Check MongoDB logs: `docker logs herolog-mongodb`
- Verify MongoDB connection string

### Migration Issues

If you encounter migration errors:

```bash
cd HeroLog.Worker
dotnet ef database drop --force  # Drop existing database
dotnet ef migrations remove      # Remove migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Development

### Adding New Features

1. Update shared models in `HeroLog.Shared/Models/`
2. Create new migration: `dotnet ef migrations add MigrationName`
3. Update API controllers or Worker services as needed
4. Build and test: `dotnet build && dotnet test`

### Running Tests

```bash
dotnet test
```

## Deployment

### Docker Deployment

You can containerize the API and Worker projects:

**Dockerfile example for API**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HeroLog.Api/HeroLog.Api.csproj", "HeroLog.Api/"]
COPY ["HeroLog.Shared/HeroLog.Shared.csproj", "HeroLog.Shared/"]
RUN dotnet restore "HeroLog.Api/HeroLog.Api.csproj"
COPY . .
WORKDIR "/src/HeroLog.Api"
RUN dotnet build "HeroLog.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HeroLog.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HeroLog.Api.dll"]
```

### Production Considerations

- Update connection strings with production credentials
- Enable authentication and authorization
- Configure HTTPS certificates
- Set up monitoring and alerting
- Implement log retention policies
- Configure RabbitMQ clustering for high availability
- Use connection pooling for databases
- Implement circuit breakers for resilience

## Technologies Used

- .NET 8.0
- ASP.NET Core Web API
- Entity Framework Core 8.0
- RabbitMQ Client 7.2.0
- MongoDB Driver 3.5.2
- Swashbuckle (Swagger/OpenAPI)
- Microsoft SQL Server
- MongoDB
- Docker & Docker Compose

## License

This project is licensed under the MIT License.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Support

For issues, questions, or contributions, please open an issue on GitHub.

## Authors

- HeroLog Development Team

---

**Happy Logging! 🦸‍♂️📝**
