using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SOPServer.API;
using SOPServer.API.Middlewares;
using SOPServer.Repository.DBContext;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Mappers;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;

if (!env.IsDevelopment())
{
    var deploymentPath = Environment.GetEnvironmentVariable("PATH_SOP");

    if (string.IsNullOrEmpty(deploymentPath))
    {
        throw new Exception("Environment variable PATH_SOP is not set.");
    }

    builder.Configuration.AddJsonFile(deploymentPath, optional: false, reloadOnChange: true);
}

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        var response = new BaseResponseModel
        {
            StatusCode = 400,
            Message = string.Join("; ", errors)
        };

        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SOP Server", Version = "v.1.0" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("app-cors",
        builder =>
        {
            builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Pagination")
            .AllowAnyMethod();
        });
});

// ===================== CONFIG REDIS CONNECTION =======================

builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection("RedisSettings"));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(
        builder.Configuration["RedisSettings:RedisConnectionString"],
        true);
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["RedisSettings:RedisConnectionString"];
    options.InstanceName = builder.Configuration["RedisSettings:InstanceName"];
});
// ======================================================================

builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection("MailSettings"));
builder.Services.AddHttpClient("SOPHttpClient", client =>
{
    // Configure default headers, timeout, etc. if needed
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Remove the "role" claim type mapping to prevent transformation to full URI
// This ensures "role" claim stays as "role" for role-based authorization to work
// We keep other default mappings to preserve standard claim behavior
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("role");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        var cfg = builder.Configuration;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(cfg["JWT:SecretKey"])
            ),

            ValidateIssuer   = true,
            ValidIssuer      = cfg["JWT:ValidIssuer"],
            ValidateAudience = true,
            ValidAudience    = cfg["JWT:ValidAudience"],
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero,
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var userIdStr = ctx.Principal?.FindFirst("UserId")?.Value;
                var jti = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(jti))
                {
                    ctx.Fail("Invalid token claims");
                    return;
                }
                
                // Log all claims for debugging
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("=== Token Claims ===");
                if (ctx.Principal != null)
                {
                    foreach (var claim in ctx.Principal.Claims)
                    {
                        logger.LogInformation($"Claim Type: {claim.Type}, Value: {claim.Value}");
                    }
                }
                logger.LogInformation("===================");
                
                var redis = ctx.HttpContext.RequestServices.GetRequiredService<IRedisService>();
                var key = RedisKeyConstants.GetAccessTokenKey(long.Parse(userIdStr), jti);
                var exists = await redis.ExistsAsync(key);
                if (!exists)
                {
                    ctx.Fail("Token revoked or expired in session store");
                }
            },
            OnChallenge = async ctx =>
            {
                // Ngăn response mặc định của JWT Bearer
                ctx.HandleResponse();

                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";

                var response = new BaseResponseModel
                {
                    StatusCode = 401,
                    Message = string.IsNullOrEmpty(ctx.ErrorDescription) 
                ? "Unauthorized - Token is missing or invalid" 
                : ctx.ErrorDescription
                };

                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                await ctx.Response.WriteAsync(jsonResponse);
            },
            OnForbidden = async ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("=== OnForbidden Event Triggered ===");
                if (ctx.Principal != null)
                {
                    logger.LogWarning("User is authenticated");
                    var roleClaim = ctx.Principal.FindFirst("role");
                    logger.LogWarning($"Role claim: {roleClaim?.Value ?? "NULL"}");
                    
                    // Check all role-related claims
                    foreach (var claim in ctx.Principal.Claims.Where(c => c.Type.ToLower().Contains("role")))
                    {
                        logger.LogWarning($"Role-related claim - Type: {claim.Type}, Value: {claim.Value}");
                    }
                }
                else
                {
                    logger.LogWarning("User is NOT authenticated (Principal is null)");
                }
                logger.LogWarning("===================================");
                
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "application/json";

                var response = new BaseResponseModel
                {
                    StatusCode = 403,
                    Message = "Forbidden - You don't have permission to access this resource"
                };

                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

                await ctx.Response.WriteAsync(jsonResponse);
            }
        };
    });

builder.Services.AddAuthorization();

// Register AutoMapper with all profiles from the Service assembly
builder.Services.AddAutoMapper(typeof(MapperConfigProfile).Assembly);

builder.Services.AddInfractstructure(builder.Configuration);

builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<FirebaseStorageSettings>(builder.Configuration.GetSection("FirebaseStorage"));
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("MinioStorage"));

// ===================== CONFIG DATABASE CONNECTION =======================

builder.Services.AddDbContext<SOPServerContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SOPServerLocal"));
});

// ==========================================================

builder.Services.AddHttpClient("RembgClient", client =>
{
    client.BaseAddress = new Uri("https://rembg.wizlab.io.vn/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SOP Server v.01");
});
try
{
    var redis = app.Services.GetRequiredService<IConnectionMultiplexer>();
    var db = redis.GetDatabase();
    await db.PingAsync();
    Console.WriteLine("Redis connection successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Redis connection failed: {ex.Message}");
}

app.UseHttpsRedirection();

app.UseCors("app-cors");

// 1. Authentication - Xác thực JWT token
app.UseAuthentication();

// 2. Kiểm tra token có tồn tại trong Redis không
app.UseMiddleware<AuthenHandlingMiddleware>();

// 3. Authorization - Phân quyền dựa trên roles
app.UseAuthorization();

// 4. Exception handling - Bắt và xử lý tất cả exceptions
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 5. Map controllers - Route đến endpoints
app.MapControllers();

app.Run();
