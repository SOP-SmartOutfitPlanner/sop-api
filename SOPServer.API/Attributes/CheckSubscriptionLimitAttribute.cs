using Microsoft.AspNetCore.Mvc.Filters;

namespace SOPServer.API.Attributes
{
    /// <summary>
    /// Attribute to check subscription limits before executing an action.
    /// Automatically checks if user can perform action and increments usage counter on success.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CheckSubscriptionLimitAttribute : Attribute, IFilterFactory
    {
        /// <summary>
        /// The usage key to track (e.g., "outfitsCreated", "wardrobeItems")
        /// </summary>
        public string UsageKey { get; }

        /// <summary>
        /// The limit key to check against (e.g., "maxOutfits", "maxWardrobeItems")
        /// </summary>
        public string LimitKey { get; }

        /// <summary>
        /// Whether to automatically increment usage counter after successful action (default: true)
        /// </summary>
        public bool AutoIncrement { get; set; } = true;

        /// <summary>
        /// Custom error message when limit is reached
        /// </summary>
        public string? ErrorMessage { get; set; }

        public bool IsReusable => false;

        public CheckSubscriptionLimitAttribute(string usageKey, string limitKey)
        {
            UsageKey = usageKey ?? throw new ArgumentNullException(nameof(usageKey));
            LimitKey = limitKey ?? throw new ArgumentNullException(nameof(limitKey));
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var filter = serviceProvider.GetRequiredService<SubscriptionLimitActionFilter>();
            filter.UsageKey = UsageKey;
            filter.LimitKey = LimitKey;
            filter.AutoIncrement = AutoIncrement;
            filter.ErrorMessage = ErrorMessage;
            return filter;
        }
    }
}
