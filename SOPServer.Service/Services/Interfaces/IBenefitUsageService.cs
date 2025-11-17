using SOPServer.Repository.Enums;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IBenefitUsageService
    {
        /// <summary>
        /// Check if user has credits remaining for a feature
        /// </summary>
        Task<bool> CanUseFeatureAsync(long userId, FeatureCode featureCode);

        /// <summary>
        /// Decrement usage credit when user uses a feature
        /// </summary>
        Task<bool> DecrementUsageAsync(long userId, FeatureCode featureCode, int amount = 1);

        /// <summary>
        /// Increment usage credit when user deletes/refunds (only for ResetType.Never)
        /// </summary>
        Task<bool> IncrementUsageAsync(long userId, FeatureCode featureCode, int amount = 1);

        /// <summary>
        /// Get current usage information for a feature
        /// </summary>
        Task<(int remainingCredits, int? totalLimit)> GetUsageInfoAsync(long userId, FeatureCode featureCode);
    }
}
