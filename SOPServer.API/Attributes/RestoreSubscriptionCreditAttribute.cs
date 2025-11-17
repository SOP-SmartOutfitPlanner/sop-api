using Microsoft.AspNetCore.Mvc.Filters;
using SOPServer.Repository.Enums;

namespace SOPServer.API.Attributes
{
    /// <summary>
    /// Attribute to restore subscription credits after successful deletion.
    /// Automatically increments usage credits when item is deleted (only for BenefitType.Persistent features).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RestoreSubscriptionCreditAttribute : Attribute, IFilterFactory
    {
        /// <summary>
        /// The feature code to restore credits for (e.g., FeatureCode.ItemWardrobe)
        /// </summary>
        public FeatureCode FeatureCode { get; }

        /// <summary>
        /// The amount of credits to restore (default: 1)
        /// </summary>
        public int Amount { get; set; } = 1;

        /// <summary>
        /// Whether to automatically increment usage counter after successful action (default: true)
        /// </summary>
        public bool AutoIncrement { get; set; } = true;

        public bool IsReusable => false;

        public RestoreSubscriptionCreditAttribute(FeatureCode featureCode)
        {
            FeatureCode = featureCode;
        }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var filter = serviceProvider.GetRequiredService<SubscriptionCreditRestoreFilter>();
            filter.FeatureCode = FeatureCode;
            filter.Amount = Amount;
            filter.AutoIncrement = AutoIncrement;
            return filter;
        }
    }
}
