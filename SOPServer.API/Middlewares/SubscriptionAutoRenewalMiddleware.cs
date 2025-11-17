using SOPServer.Service.Services.Interfaces;

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
            // Only check for authenticated users
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst("UserId")?.Value;
                if (long.TryParse(userIdClaim, out long userId))
                {
                    try
                    {
                        // Ensure user has active subscription (auto-renew if needed)
                        // Uses Redis caching to minimize DB overhead
                        await subscriptionService.EnsureUserHasActiveSubscriptionAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't block request
                        // Subscription checks in individual endpoints will catch any issues
                        Console.WriteLine($"Error in SubscriptionAutoRenewalMiddleware: {ex.Message}");
                    }
                }
            }

            // Continue to next middleware
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
