using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserSubscriptionModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IUserSubscriptionService
    {
        /// <summary>
        /// Purchase a subscription plan
        /// </summary>
        Task<BaseResponseModel> PurchaseSubscriptionAsync(long userId, PurchaseSubscriptionRequestModel model);

        /// <summary>
        /// Get user's current active subscription
        /// </summary>
        Task<BaseResponseModel> GetMySubscriptionAsync(long userId);

        /// <summary>
        /// Get all available subscription plans (for users to browse)
        /// </summary>
        Task<BaseResponseModel> GetAvailablePlansAsync();

        /// <summary>
        /// Get user's subscription history
        /// </summary>
        Task<BaseResponseModel> GetSubscriptionHistoryAsync(long userId);
    }
}
