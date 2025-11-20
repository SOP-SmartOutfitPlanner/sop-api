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
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var roleClaim = context.User.FindFirst("role")?.Value;
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
