using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserSubscriptionModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IUserSubscriptionService
    {
        /// <summary>
        /// Purchase a subscription plan (creates pending subscription and returns payment URL)
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

        /// <summary>
        /// Process payment webhook to activate subscription after successful payment
        /// </summary>
        Task<BaseResponseModel> ProcessPaymentWebhookAsync(long transactionId, string paymentStatus);

        /// <summary>
        /// Ensure user has active subscription, auto-renew free plan if expired/missing
        /// </summary>
        Task EnsureUserHasActiveSubscriptionAsync(long userId);
    }
}
