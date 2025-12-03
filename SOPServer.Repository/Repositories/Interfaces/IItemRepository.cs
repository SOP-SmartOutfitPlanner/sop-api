using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IItemRepository : IGenericRepository<Item>
    {
        Task<bool> ExistsByNameAsync(string name, long userId, long? excludeId = null);
        Task<int> CountItemByUserId(long userId);
        Task<int> CountAnalyzedItemByUserId(long userId);
        Task<int> CountSystemItems();
        Task<int> CountAnalyzedSystemItems();
        Task<int> CountItemByUserIdAndCategoryParent(long userId, long category);
        
        /// <summary>
        /// Get items by their IDs with all related data included
        /// </summary>
        Task<List<Item>> GetItemsByIdsAsync(List<long> itemIds);

        /// <summary>
        /// Get items that have any of the specified Season IDs
        /// </summary>
        Task<List<Item>> GetItemsBySeasonIdsAsync(List<long> seasonIds, long? userId = null);

        /// <summary>
        /// Get items that have any of the specified Occasion IDs
        /// </summary>
        Task<List<Item>> GetItemsByOccasionIdsAsync(List<long> occasionIds, long? userId = null);

        /// <summary>
        /// Get items that have any of the specified Style IDs
        /// </summary>
        Task<List<Item>> GetItemsByStyleIdsAsync(List<long> styleIds, long? userId = null);

        /// <summary>
        /// Get items that have any of the specified Season IDs, excluding specified item IDs
        /// </summary>
        Task<List<Item>> GetItemsBySeasonIdsAsync(List<long> seasonIds, List<long> excludeIds, long? userId = null);

        /// <summary>
        /// Get items that have any of the specified Occasion IDs, excluding specified item IDs
        /// </summary>
        Task<List<Item>> GetItemsByOccasionIdsAsync(List<long> occasionIds, List<long> excludeIds, long? userId = null);

        /// <summary>
        /// Get items that have any of the specified Style IDs, excluding specified item IDs
        /// </summary>
        Task<List<Item>> GetItemsByStyleIdsAsync(List<long> styleIds, List<long> excludeIds, long? userId = null);

        /// <summary>
        /// Get items that have any of the specified Season IDs, excluding specified item IDs and filtering by gapDay
        /// </summary>
        Task<List<Item>> GetItemsBySeasonIdsAsync(List<long> seasonIds, List<long> excludeIds, long? userId, int? gapDay, DateTime? targetDate);

        /// <summary>
        /// Get items that have any of the specified Occasion IDs, excluding specified item IDs and filtering by gapDay
        /// </summary>
        Task<List<Item>> GetItemsByOccasionIdsAsync(List<long> occasionIds, List<long> excludeIds, long? userId, int? gapDay, DateTime? targetDate);

        /// <summary>
        /// Get items that have any of the specified Style IDs, excluding specified item IDs and filtering by gapDay
        /// </summary>
        Task<List<Item>> GetItemsByStyleIdsAsync(List<long> styleIds, List<long> excludeIds, long? userId, int? gapDay, DateTime? targetDate);
    }
}
