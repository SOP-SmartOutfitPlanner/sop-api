using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Attributes
{
    /// <summary>
    /// Action filter that restores subscription credits after successful deletion
    /// </summary>
    public class SubscriptionCreditRestoreFilter : IAsyncActionFilter
    {
        private readonly IBenefitUsageService _benefitUsageService;

        public FeatureCode FeatureCode { get; set; }
        public int Amount { get; set; } = 1;
        public bool AutoIncrement { get; set; } = true;

        public SubscriptionCreditRestoreFilter(IBenefitUsageService benefitUsageService)
        {
            _benefitUsageService = benefitUsageService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Execute the action first (e.g., delete item)
            var executedContext = await next();

            // Only increment if action succeeded and AutoIncrement is true
            if (AutoIncrement && executedContext.Exception == null)
            {
                // Get user ID from claims
                var userIdClaim = context.HttpContext.User.FindFirst("UserId")?.Value;

                if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var userId))
                {
                    // Check if the response is successful (2xx status code)
                    bool shouldRestore = false;

                    if (executedContext.Result is ObjectResult objectResult)
                    {
                        if (objectResult.Value is BaseResponseModel baseResponse &&
                            baseResponse.StatusCode >= 200 && baseResponse.StatusCode < 300)
                        {
                            shouldRestore = true;
                        }
                    }
                    else if (executedContext.Result is OkObjectResult ||
                             executedContext.Result is NoContentResult ||
                             executedContext.Result is OkResult)
                    {
                        shouldRestore = true;
                    }

                    if (shouldRestore)
                    {
                        // Restore credits (only works for ResetType.Never features)
                        await _benefitUsageService.IncrementUsageAsync(userId, FeatureCode, Amount);
                    }
                }
            }
        }
    }
}
