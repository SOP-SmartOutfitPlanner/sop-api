using SOPServer.API.Configurations;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;

// Configuration Setup
builder.Configuration.AddConfigurationSetup(env);

// API Configuration (Controllers, JSON, Model Validation)
builder.Services.AddApiConfiguration();

// Swagger Configuration
builder.Services.AddSwaggerConfiguration();

// CORS Configuration
builder.Services.AddCorsConfiguration();

// Redis Configuration
builder.Services.AddRedisConfiguration(builder.Configuration);

// HTTP Clients Configuration
builder.Services.AddHttpClientConfiguration();

// Authentication & Authorization Configuration
builder.Services.AddAuthenticationConfiguration(builder.Configuration, builder.Environment);

// Service Configuration (AutoMapper, Settings, DI)
builder.Services.AddServiceConfiguration(builder.Configuration);

// Database Configuration
builder.Services.AddDatabaseConfiguration(builder.Configuration);

// FirebaseApp Configuration
FirebaseAppConfiguration.AddFirebaseAppConfiguration(env);

// Turn off EF Core SQL Command Logging
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.None);

var app = builder.Build();

// Swagger UI
app.UseSwaggerConfiguration();

// Redis Health Check
await app.UseRedisHealthCheckAsync();

// Qdrant Collection Initialization
await app.UseQDrantInitializationAsync();

// Middleware Pipeline (HTTPS, CORS, Authentication, Authorization, Exception Handling)
app.UseMiddlewareConfiguration(CorsConfiguration.PolicyName);

// Map Controllers
app.MapControllers();

app.Run();
