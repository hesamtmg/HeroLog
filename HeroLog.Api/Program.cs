using HeroLog.Api.Services;
using HeroLog.Shared.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "HeroLog.Api", Version = "v1", Description = "API for publishing service logs to RabbitMQ" });
});

// Configure RabbitMQ settings
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

// Register RabbitMQ producer as singleton
builder.Services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();

// Configure CORS (optional)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HeroLog.Api v1"));
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
