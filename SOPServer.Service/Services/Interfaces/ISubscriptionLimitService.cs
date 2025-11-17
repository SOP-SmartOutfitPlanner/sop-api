namespace SOPServer.Service.Services.Interfaces
{
    public interface ISubscriptionLimitService
    {
        /// <summary>
        /// Check if user can perform an action based on their subscription limit
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="usageKey">Usage key (e.g., "outfitsCreated", "wardrobeItems")</param>
        /// <param name="limitKey">Limit key (e.g., "maxOutfits", "maxWardrobeItems")</param>
        /// <returns>True if user can perform action, false otherwise</returns>
        Task<bool> CanPerformActionAsync(long userId, string usageKey, string limitKey);

        /// <summary>
        /// Check if user has access to a specific feature
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="featureName">Feature name (e.g., "aiRecommendations", "weatherIntegration")</param>
        /// <returns>True if user has access, false otherwise</returns>
        Task<bool> HasFeatureAccessAsync(long userId, string featureName);

        /// <summary>
        /// Increment usage counter for a specific action
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="usageKey">Usage key</param>
        /// <param name="amount">Amount to increment (default: 1)</param>
        Task IncrementUsageAsync(long userId, string usageKey, int amount = 1);

        /// <summary>
        /// Decrement usage counter for a specific action (e.g., when item is deleted)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="usageKey">Usage key</param>
        /// <param name="amount">Amount to decrement (default: 1)</param>
        Task DecrementUsageAsync(long userId, string usageKey, int amount = 1);

        /// <summary>
        /// Get user's current subscription usage and limits
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Tuple of (usage, limit)</returns>
        Task<(int currentUsage, int? limit)> GetUsageInfoAsync(long userId, string usageKey, string limitKey);
    }
}
