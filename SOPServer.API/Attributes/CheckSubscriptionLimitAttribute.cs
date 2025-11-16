using Microsoft.AspNetCore.Mvc.Filters;
using SOPServer.Repository.Enums;

namespace SOPServer.API.Attributes
{
    /// <summary>
    /// Attribute to check subscription limits before executing an action.
    /// Automatically checks if user has credits and decrements usage on success.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CheckSubscriptionLimitAttribute : Attribute, IFilterFactory
    {
        /// <summary>
        /// The feature code to check (e.g., FeatureCode.ItemWardrobe, FeatureCode.OutfitSuggestion)
        /// </summary>
        public FeatureCode FeatureCode { get; }

        /// <summary>
        /// Whether to automatically decrement usage counter after successful action (default: true)
        /// </summary>
        public bool AutoDecrement { get; set; } = true;

        /// <summary>
        /// Custom error message when limit is reached
        /// </summary>
        public string? ErrorMessage { get; set; }

        public bool IsReusable => false;

        public CheckSubscriptionLimitAttribute(FeatureCode featureCode)
        {
            FeatureCode = featureCode;
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var filter = serviceProvider.GetRequiredService<SubscriptionLimitActionFilter>();
            filter.FeatureCode = FeatureCode;
            filter.AutoDecrement = AutoDecrement;
            filter.ErrorMessage = ErrorMessage;
            return filter;
        }
    }
}
