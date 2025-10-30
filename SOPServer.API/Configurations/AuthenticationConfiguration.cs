using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SOPServer.API.Configurations
{
    public static class AuthenticationConfiguration
    {
        public static IServiceCollection AddAuthenticationConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"])
                    ),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:ValidAudience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role
                };

                // JWT Token validation events
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

            services.AddAuthorization();

            return services;
        }
    }
}
