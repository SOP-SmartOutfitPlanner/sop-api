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
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

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

        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SOP Server", Version = "v.1.0" });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
      c.IncludeXmlComments(xmlPath);
    }

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

// Service Configuration (AutoMapper, Settings, DI)
builder.Services.AddServiceConfiguration(builder.Configuration);

// Database Configuration
builder.Services.AddDatabaseConfiguration(builder.Configuration);

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
                var redis = ctx.HttpContext.RequestServices.GetRequiredService<IRedisService>();
                var key = RedisKeyConstants.GetAccessTokenKey(long.Parse(userIdStr), jti);
                var exists = await redis.ExistsAsync(key);
                if (!exists)
                {
                    ctx.Fail("Token revoked or expired in session store");
                }
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddAutoMapper(typeof(Program));
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

// Swagger UI
app.UseSwaggerConfiguration();

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
