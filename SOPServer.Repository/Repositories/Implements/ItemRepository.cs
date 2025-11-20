using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class ItemRepository : GenericRepository<Item>, IItemRepository
    {
        private readonly SOPServerContext _context;
        public ItemRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> CountItemByUserId(long userId)
        {
            return await _context.Items.CountAsync(x => !x.IsDeleted && x.UserId == userId);
        }

        public async Task<int> CountItemByUserIdAndCategoryParent(long userId, long categoryId)
        {
            return await _context.Items
                .Where(x => !x.IsDeleted && x.UserId == userId)
                .Include(i => i.Category)
                .Where(i => i.Category != null && i.Category.ParentId == categoryId)
                .CountAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, long userId, long? excludeId = null)
        {
            var query = _context.Items.Where(x => !x.IsDeleted && x.UserId == userId && x.Name == name);

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            return await query.AnyAsync();
        }

        public async Task<List<Item>> GetItemsByIdsAsync(List<long> itemIds)
        {
            if (itemIds == null || !itemIds.Any())
                return new List<Item>();

            return await _context.Items
                .Where(x => !x.IsDeleted && itemIds.Contains(x.Id))
                .Include(x => x.Category)
                .Include(x => x.User)
                .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                .ToListAsync();
        }

        public async Task<List<Item>> QueryItemsByCriteriaAsync(
            long userId,
            string? category = null,
            List<string>? styles = null,
            List<string>? occasions = null,
            List<string>? seasons = null,
            List<string>? colors = null,
            string? weatherSuitable = null,
            int maxResults = 10)
        {
            var query = _context.Items
                .Where(x => !x.IsDeleted && x.UserId == userId)
                .Include(x => x.Category)
                .Include(x => x.User)
                .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                .AsQueryable();

            // Filter by category name (case-insensitive)
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category != null && 
                    x.Category.Name.ToLower().Contains(category.ToLower()));
            }

            // Filter by styles (any match)
            if (styles != null && styles.Any())
            {
                var lowerStyles = styles.Select(s => s.ToLower()).ToList();
                query = query.Where(x => x.ItemStyles.Any(ist => 
                    ist.Style != null && lowerStyles.Contains(ist.Style.Name.ToLower())));
            }

            // Filter by occasions (any match)
            if (occasions != null && occasions.Any())
            {
                var lowerOccasions = occasions.Select(o => o.ToLower()).ToList();
                query = query.Where(x => x.ItemOccasions.Any(io => 
                    io.Occasion != null && lowerOccasions.Contains(io.Occasion.Name.ToLower())));
            }

            // Filter by seasons (any match)
            if (seasons != null && seasons.Any())
            {
                var lowerSeasons = seasons.Select(s => s.ToLower()).ToList();
                query = query.Where(x => x.ItemSeasons.Any(ise => 
                    ise.Season != null && lowerSeasons.Contains(ise.Season.Name.ToLower())));
            }

            // Filter by weather suitable (case-insensitive contains)
            if (!string.IsNullOrWhiteSpace(weatherSuitable))
            {
                query = query.Where(x => x.WeatherSuitable != null && 
                    x.WeatherSuitable.ToLower().Contains(weatherSuitable.ToLower()));
            }

            // Filter by colors (check if any of the requested colors exist in the item's color JSON)
            if (colors != null && colors.Any())
            {
                var lowerColors = colors.Select(c => c.ToLower()).ToList();
                query = query.Where(x => x.Color != null && 
                    lowerColors.Any(color => x.Color.ToLower().Contains(color)));
            }

            // Order by analyzed confidence and take max results
            return await query
                .OrderByDescending(x => x.AIConfidence ?? 0)
                .ThenByDescending(x => x.LastWornAt)
                .Take(maxResults)
                .ToListAsync();
        }
    }
}
