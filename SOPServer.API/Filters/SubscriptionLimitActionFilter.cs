using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Attributes
{
    /// <summary>
    /// Action filter that checks subscription limits before executing an action
    /// </summary>
    public class SubscriptionLimitActionFilter : IAsyncActionFilter
    {
        private readonly IBenefitUsageService _benefitUsageService;

        public FeatureCode FeatureCode { get; set; }
        public bool AutoDecrement { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public SubscriptionLimitActionFilter(IBenefitUsageService benefitUsageService)
        {
            _benefitUsageService = benefitUsageService;
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

            // Check if user can use this feature (has remaining credits)
            var canUse = await _benefitUsageService.CanUseFeatureAsync(userId, FeatureCode);

            if (!canUse)
            {
                // Get usage info for detailed error message
                var (remainingCredits, totalLimit) = await _benefitUsageService.GetUsageInfoAsync(userId, FeatureCode);

                var message = ErrorMessage ??
                    $"Subscription limit reached for {FeatureCode}. You have {remainingCredits} credits remaining out of {totalLimit}. Please upgrade your subscription plan.";

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

            // Only decrement if action succeeded and AutoDecrement is true
            if (AutoDecrement && executedContext.Exception == null)
            {
                // Check if the response is successful (2xx status code)
                if (executedContext.Result is ObjectResult objectResult)
                {
                    if (objectResult.Value is BaseResponseModel baseResponse &&
                        baseResponse.StatusCode >= 200 && baseResponse.StatusCode < 300)
                    {
                        await _benefitUsageService.DecrementUsageAsync(userId, FeatureCode);
                    }
                }
                else if (executedContext.Result is OkObjectResult ||
                         executedContext.Result is CreatedResult ||
                         executedContext.Result is CreatedAtActionResult)
                {
                    await _benefitUsageService.DecrementUsageAsync(userId, FeatureCode);
                }
            }
        }
    }
}
