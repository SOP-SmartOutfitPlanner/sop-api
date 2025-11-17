namespace SOPServer.Service.BusinessModels.SubscriptionLimitModels
{
    public class SubscriptionBenefitLimit
    {
        public int? MaxOutfits { get; set; }
        public int? MaxWardrobeItems { get; set; }
        public int? MaxCollections { get; set; }
        public int? MaxPosts { get; set; }
        public int? MaxOccasions { get; set; }
        public bool HasAIRecommendations { get; set; }
        public bool HasWeatherIntegration { get; set; }
        public bool HasPremiumStyles { get; set; }
        public bool HasAdvancedAnalytics { get; set; }

        // null means unlimited
        public bool IsUnlimited(string limitKey) => GetLimitValue(limitKey) == null;

        public int? GetLimitValue(string limitKey)
        {
            return limitKey switch
            {
                "maxOutfits" => MaxOutfits,
                "maxWardrobeItems" => MaxWardrobeItems,
                "maxCollections" => MaxCollections,
                "maxPosts" => MaxPosts,
                "maxOccasions" => MaxOccasions,
                _ => null
            };
        }

        public bool HasFeature(string featureName)
        {
            return featureName switch
            {
                "aiRecommendations" => HasAIRecommendations,
                "weatherIntegration" => HasWeatherIntegration,
                "premiumStyles" => HasPremiumStyles,
                "advancedAnalytics" => HasAdvancedAnalytics,
                _ => false
            };
        }
    }
}
