using SOPServer.Service.Services.Interfaces;
using System.Security.Claims;

namespace SOPServer.API.Middlewares
{
    /// <summary>
    /// Middleware that ensures authenticated users have an active subscription
    /// Auto-creates free plan subscription if user has none or expired
    /// </summary>
    public class SubscriptionAutoRenewalMiddleware
    {
        private readonly RequestDelegate _next;

        public SubscriptionAutoRenewalMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserSubscriptionService subscriptionService)
        {
            // Skip middleware for subscription purchase/management endpoints to avoid creating duplicate subscriptions
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var isSubscriptionEndpoint = path.Contains("/api/v1/subscriptions/purchase")
                                      || path.Contains("/api/v1/subscriptions/cancel");

            if (isSubscriptionEndpoint)
            {
                await _next(context);
                return;
            }

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (roleClaim == "ADMIN")
                {
                    await _next(context);
                    return;
                }

                var userIdClaim = context.User.FindFirst("UserId")?.Value;
                if (long.TryParse(userIdClaim, out long userId))
                {
                    try
                    {
                        await subscriptionService.EnsureUserHasActiveSubscriptionAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in SubscriptionAutoRenewalMiddleware: {ex.Message}");
                    }
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to register the middleware
    /// </summary>
    public static class SubscriptionAutoRenewalMiddlewareExtensions
    {
        public static IApplicationBuilder UseSubscriptionAutoRenewal(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SubscriptionAutoRenewalMiddleware>();
        }
    }
}
