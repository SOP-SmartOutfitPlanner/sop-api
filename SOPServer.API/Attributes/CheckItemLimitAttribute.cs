using Microsoft.AspNetCore.Mvc.Filters;

namespace SOPServer.API.Attributes
{
    /// <summary>
    /// Attribute to check item count limit based on actual database query.
    /// More reliable than credit-based system for capacity limits.
    /// Validates against subscription plan's ItemWardrobe limit.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CheckItemLimitAttribute : Attribute, IFilterFactory
    {
        /// <summary>
        /// Number of items being created (default: 1 for single item creation)
        /// For bulk operations, set this dynamically based on request
        /// </summary>
        public int ItemCount { get; set; } = 1;

        /// <summary>
        /// Custom error message when limit is reached
        /// </summary>
        public string? ErrorMessage { get; set; }

        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var filter = serviceProvider.GetRequiredService<ItemLimitActionFilter>();
            filter.ItemCount = ItemCount;
            filter.ErrorMessage = ErrorMessage;
            return filter;
        }
    }
}
