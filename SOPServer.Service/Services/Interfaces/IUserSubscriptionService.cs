using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserSubscriptionModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IUserSubscriptionService
    {
        Task<BaseResponseModel> PurchaseSubscriptionAsync(long userId, PurchaseSubscriptionRequestModel model);

        Task<BaseResponseModel> GetMySubscriptionAsync(long userId);

        Task<BaseResponseModel> GetAvailablePlansAsync();

        Task<BaseResponseModel> GetSubscriptionHistoryAsync(long userId);

        Task<BaseResponseModel> ProcessPaymentWebhookAsync(long transactionId, string paymentStatus);

        Task EnsureUserHasActiveSubscriptionAsync(long userId);

        Task<BaseResponseModel> CancelPendingPaymentAsync(long userId, long transactionId);
    }
}
