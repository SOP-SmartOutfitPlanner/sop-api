using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Middlewares
{
    public class AuthenHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public AuthenHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check Authorization header for Bearer token
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (!string.IsNullOrEmpty(token))
                {
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(token);

                        var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                        var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "UserId" || c.Type == ClaimTypes.NameIdentifier)?.Value;

                        if (!string.IsNullOrEmpty(jti) && !string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var userId))
                        {
                            var redis = context.RequestServices.GetService<IRedisService>();
                            if (redis != null)
                            {
                                var key = RedisKeyConstants.GetAccessTokenKey(userId, jti);
                                var exists = await redis.ExistsAsync(key);
                                if (!exists)
                                {
                                    context.Response.ContentType = "application/json";
                                    context.Response.StatusCode = StatusCodes.Status403Forbidden;

                                    var response = new BaseResponseModel
                                    {
                                        StatusCode = StatusCodes.Status403Forbidden,
                                        Message = MessageConstants.TOKEN_NOT_VALID
                                    };

                                    var jsonResponse = JsonConvert.SerializeObject(response, new JsonSerializerSettings
                                    {
                                        ContractResolver = new DefaultContractResolver
                                        {
                                            NamingStrategy = new CamelCaseNamingStrategy()
                                        }
                                    });

                                    await context.Response.WriteAsync(jsonResponse);
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse/validate bearer token");
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;

                        var response = new BaseResponseModel
                        {
                            StatusCode = StatusCodes.Status403Forbidden,
                            Message = MessageConstants.TOKEN_NOT_VALID
                        };

                        var jsonResponse = JsonConvert.SerializeObject(response, new JsonSerializerSettings
                        {
                            ContractResolver = new DefaultContractResolver
                            {
                                NamingStrategy = new CamelCaseNamingStrategy()
                            }
                            });

                        await context.Response.WriteAsync(jsonResponse);
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
