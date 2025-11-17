using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SOPServer.Repository.Enums;
using SOPServer.Repository.Repositories.Interfaces;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Attributes
{
    /// <summary>
    /// Action filter that validates item count against subscription limits using actual DB count.
    /// More reliable than credit-based system for preventing limit bypass.
    /// </summary>
    public class ItemLimitActionFilter : IAsyncActionFilter
    {
        private readonly IItemRepository _itemRepository;
        private readonly IBenefitUsageService _benefitUsageService;

        public int ItemCount { get; set; } = 1;
        public string? ErrorMessage { get; set; }

        public ItemLimitActionFilter(IItemRepository itemRepository, IBenefitUsageService benefitUsageService)
        {
            _itemRepository = itemRepository;
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

            // For bulk operations, extract the actual count from the request
            int itemsToCreate = ItemCount;

            // Check if this is a bulk operation and extract count
            if (context.ActionArguments.TryGetValue("bulkUploadModel", out var bulkModel) && bulkModel != null)
            {
                try
                {
                    var modelType = bulkModel.GetType();

                    // Try to get count from ImageURLs property
                    var imageUrlsProperty = modelType.GetProperty("ImageURLs");
                    if (imageUrlsProperty != null)
                    {
                        var imageUrls = imageUrlsProperty.GetValue(bulkModel) as System.Collections.ICollection;
                        if (imageUrls != null)
                        {
                            itemsToCreate = imageUrls.Count;
                        }
                    }
                    else
                    {
                        // Try to get count from ItemsUpload property
                        var itemsUploadProperty = modelType.GetProperty("ItemsUpload");
                        if (itemsUploadProperty != null)
                        {
                            var itemsUpload = itemsUploadProperty.GetValue(bulkModel) as System.Collections.ICollection;
                            if (itemsUpload != null)
                            {
                                itemsToCreate = itemsUpload.Count;
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to default if reflection fails
                    itemsToCreate = ItemCount;
                }
            }

            // Get current item count from database (source of truth)
            var currentItemCount = await _itemRepository.CountItemByUserId(userId);

            // Get user's subscription limit
            var (remainingCredits, totalLimit) = await _benefitUsageService.GetUsageInfoAsync(userId, FeatureCode.ItemWardrobe);

            if (totalLimit == null)
            {
                // User has no subscription or limit is not set
                var message = "No active subscription found. Please subscribe to a plan to add items.";
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

            // Check if adding these items would exceed the limit
            if (currentItemCount + itemsToCreate > totalLimit.Value)
            {
                var message = ErrorMessage ??
                    $"Item limit reached. You currently have {currentItemCount} items and your limit is {totalLimit.Value}. " +
                    $"Adding {itemsToCreate} more item(s) would exceed your limit. Please upgrade your subscription plan.";

                context.Result = new ObjectResult(new BaseResponseModel
                {
                    Message = message,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Data = new
                    {
                        CurrentCount = currentItemCount,
                        Limit = totalLimit.Value,
                        RequestedCount = itemsToCreate,
                        Available = totalLimit.Value - currentItemCount
                    }
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            // Proceed with the action if limit check passes
            await next();
        }
    }
}
