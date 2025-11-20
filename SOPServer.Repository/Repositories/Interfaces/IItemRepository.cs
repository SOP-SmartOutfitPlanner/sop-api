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
        Task<int> CountItemByUserIdAndCategoryParent(long userId, long category);
        
        /// <summary>
        /// Get items by their IDs with all related data included
        /// </summary>
        Task<List<Item>> GetItemsByIdsAsync(List<long> itemIds);

        /// <summary>
        /// Query items for a user based on multiple criteria (category, styles, occasions, seasons, colors)
        /// </summary>
        Task<List<Item>> QueryItemsByCriteriaAsync(
            long userId,
            string? category = null,
            List<string>? styles = null,
            List<string>? occasions = null,
            List<string>? seasons = null,
            List<string>? colors = null,
            string? weatherSuitable = null,
            int maxResults = 10);
    }
}
