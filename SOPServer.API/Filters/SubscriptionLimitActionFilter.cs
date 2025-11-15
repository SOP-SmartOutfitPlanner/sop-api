using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Attributes
{
    /// <summary>
    /// Action filter that checks subscription limits before executing an action
    /// </summary>
    public class SubscriptionLimitActionFilter : IAsyncActionFilter
    {
        private readonly ISubscriptionLimitService _subscriptionLimitService;

        public string UsageKey { get; set; } = string.Empty;
        public string LimitKey { get; set; } = string.Empty;
        public bool AutoIncrement { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public SubscriptionLimitActionFilter(ISubscriptionLimitService subscriptionLimitService)
        {
            _subscriptionLimitService = subscriptionLimitService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Get user ID from claims
            var userIdClaim = context.HttpContext.User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                // If user is not authenticated, let the [Authorize] attribute handle it
                await next();
                return;
            }

            // Check if user can perform action
            var canPerform = await _subscriptionLimitService.CanPerformActionAsync(userId, UsageKey, LimitKey);

            if (!canPerform)
            {
                // Get usage info for detailed error message
                var (currentUsage, limit) = await _subscriptionLimitService.GetUsageInfoAsync(userId, UsageKey, LimitKey);

                var message = ErrorMessage ??
                    $"Subscription limit reached. You have used {currentUsage} out of {limit} allowed. Please upgrade your subscription plan.";

                context.Result = new ObjectResult(new BaseResponseModel
                {
                    Message = message,
                    StatusCode = StatusCodes.Status403Forbidden
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // Execute the action
            var executedContext = await next();

            // Only increment if action succeeded and AutoIncrement is true
            if (AutoIncrement && executedContext.Exception == null)
            {
                // Check if the response is successful (2xx status code)
                if (executedContext.Result is ObjectResult objectResult)
                {
                    if (objectResult.Value is BaseResponseModel baseResponse &&
                        baseResponse.StatusCode >= 200 && baseResponse.StatusCode < 300)
                    {
                        await _subscriptionLimitService.IncrementUsageAsync(userId, UsageKey);
                    }
                }
                else if (executedContext.Result is OkObjectResult ||
                         executedContext.Result is CreatedResult ||
                         executedContext.Result is CreatedAtActionResult)
                {
                    await _subscriptionLimitService.IncrementUsageAsync(userId, UsageKey);
                }
            }
        }
    }
}
