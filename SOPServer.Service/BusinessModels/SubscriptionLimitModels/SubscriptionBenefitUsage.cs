namespace SOPServer.Service.BusinessModels.SubscriptionLimitModels
{
    public class SubscriptionBenefitUsage
    {
        public int OutfitsCreated { get; set; }
        public int WardrobeItemsCount { get; set; }
        public int CollectionsCount { get; set; }
        public int PostsCount { get; set; }
        public int OccasionsCount { get; set; }

        public int GetUsageValue(string usageKey)
        {
            return usageKey switch
            {
                "outfitsCreated" => OutfitsCreated,
                "wardrobeItems" => WardrobeItemsCount,
                "collections" => CollectionsCount,
                "posts" => PostsCount,
                "occasions" => OccasionsCount,
                _ => 0
            };
        }

        public void IncrementUsage(string usageKey, int amount = 1)
        {
            switch (usageKey)
            {
                case "outfitsCreated":
                    OutfitsCreated += amount;
                    break;
                case "wardrobeItems":
                    WardrobeItemsCount += amount;
                    break;
                case "collections":
                    CollectionsCount += amount;
                    break;
                case "posts":
                    PostsCount += amount;
                    break;
                case "occasions":
                    OccasionsCount += amount;
                    break;
            }
        }

        public void DecrementUsage(string usageKey, int amount = 1)
        {
            switch (usageKey)
            {
                case "outfitsCreated":
                    OutfitsCreated = Math.Max(0, OutfitsCreated - amount);
                    break;
                case "wardrobeItems":
                    WardrobeItemsCount = Math.Max(0, WardrobeItemsCount - amount);
                    break;
                case "collections":
                    CollectionsCount = Math.Max(0, CollectionsCount - amount);
                    break;
                case "posts":
                    PostsCount = Math.Max(0, PostsCount - amount);
                    break;
                case "occasions":
                    OccasionsCount = Math.Max(0, OccasionsCount - amount);
                    break;
            }
        }
    }
}
