using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SOPServer.API.Middlewares
{
    public class AuthenHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenHandlingMiddleware> _logger;

        public AuthenHandlingMiddleware(RequestDelegate next, ILogger<AuthenHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null) { await _next(context); return; }

            var authorizeMetadata = endpoint.Metadata.GetMetadata<IAuthorizeData>();
            var allowAnonymousMetadata = endpoint.Metadata.GetMetadata<IAllowAnonymous>();

            if (allowAnonymousMetadata != null || authorizeMetadata == null)
            {
                await _next(context); // Skip Redis for public endpoints
                return;
            }
            // Check Authorization header for Bearer token
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (!string.IsNullOrEmpty(token))
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
                                // Không set StatusCode ở đây, để ExceptionHandlingMiddleware xử lý
                                throw new ForbiddenException(MessageConstants.TOKEN_NOT_VALID);
                            }
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
