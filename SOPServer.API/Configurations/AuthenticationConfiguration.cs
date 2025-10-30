using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
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
                    RoleClaimType = "role"
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
                    }
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}
