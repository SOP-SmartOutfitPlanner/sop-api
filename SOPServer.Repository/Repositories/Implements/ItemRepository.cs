using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
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

        public async Task<List<Item>> GetItemsBySeasonIdsAsync(List<long> seasonIds, long? userId = null)
        {
            if (seasonIds == null || !seasonIds.Any())
                return new List<Item>();

            var query = _context.Items
                .Where(x => !x.IsDeleted &&
                           x.ItemSeasons.Any(ise => !ise.IsDeleted && ise.SeasonId.HasValue && seasonIds.Contains(ise.SeasonId.Value)));

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId || x.ItemType == ItemType.SYSTEM);

            var items = await query
                .Include(x => x.Category)
                .Include(x => x.User)
                .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                .ToListAsync();

            if (items.Count <= 10)
                return items;

            return items.OrderBy(x => Guid.NewGuid()).Take(10).ToList();
        }

        public async Task<List<Item>> GetItemsByOccasionIdsAsync(List<long> occasionIds, long? userId = null)
        {
            if (occasionIds == null || !occasionIds.Any())
                return new List<Item>();

            var query = _context.Items
                .Where(x => !x.IsDeleted &&
                           x.ItemOccasions.Any(io => !io.IsDeleted && io.OccasionId.HasValue && occasionIds.Contains(io.OccasionId.Value)));

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId || x.ItemType == ItemType.SYSTEM);

            var items = await query
                .Include(x => x.Category)
                .Include(x => x.User)
                .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                .ToListAsync();

            if (items.Count <= 5)
                return items;

            return items.OrderBy(x => Guid.NewGuid()).Take(5).ToList();
        }

        public async Task<List<Item>> GetItemsByStyleIdsAsync(List<long> styleIds, long? userId = null)
        {
            if (styleIds == null || !styleIds.Any())
                return new List<Item>();

            var query = _context.Items
                .Where(x => !x.IsDeleted &&
                           x.ItemStyles.Any(ist => !ist.IsDeleted && ist.StyleId.HasValue && styleIds.Contains(ist.StyleId.Value)));

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId || x.ItemType == ItemType.SYSTEM);

            var items = await query
                .Include(x => x.Category)
                .Include(x => x.User)
                .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                .ToListAsync();

            if (items.Count <= 5)
                return items;

            return items.OrderBy(x => Guid.NewGuid()).Take(5).ToList();
        }

        public async Task<List<Item>> GetItemsBySeasonIdsAsync(List<long> seasonIds, List<long> excludeIds, long? userId = null)
        {
            if (seasonIds == null || !seasonIds.Any())
                return new List<Item>();

            var query = _context.Items
                .Where(x => !x.IsDeleted &&
                           x.ItemSeasons.Any(ise => !ise.IsDeleted && ise.SeasonId.HasValue && seasonIds.Contains(ise.SeasonId.Value)));

            if (excludeIds != null && excludeIds.Any())
                query = query.Where(x => !excludeIds.Contains(x.Id));

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId || x.ItemType == ItemType.SYSTEM);

            var items = await query
                .Include(x => x.Category)
                .Include(x => x.User)
                .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                .ToListAsync();

            if (items.Count <= 10)
                return items;

            return items.OrderBy(x => Guid.NewGuid()).Take(10).ToList();
        }

        public async Task<List<Item>> GetItemsByOccasionIdsAsync(List<long> occasionIds, List<long> excludeIds, long? userId = null)
        {
            if (occasionIds == null || !occasionIds.Any())
                return new List<Item>();

            var query = _context.Items
                .Where(x => !x.IsDeleted &&
                           x.ItemOccasions.Any(io => !io.IsDeleted && io.OccasionId.HasValue && occasionIds.Contains(io.OccasionId.Value)));

            if (excludeIds != null && excludeIds.Any())
                query = query.Where(x => !excludeIds.Contains(x.Id));

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId || x.ItemType == ItemType.SYSTEM);

            var items = await query
                .Include(x => x.Category)
                .Include(x => x.User)
                .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                .ToListAsync();

            if (items.Count <= 10)
                return items;

            return items.OrderBy(x => Guid.NewGuid()).Take(10).ToList();
        }

        public async Task<List<Item>> GetItemsByStyleIdsAsync(List<long> styleIds, List<long> excludeIds, long? userId = null)
        {
            if (styleIds == null || !styleIds.Any())
                return new List<Item>();

            var query = _context.Items
                .Where(x => !x.IsDeleted &&
                           x.ItemStyles.Any(ist => !ist.IsDeleted && ist.StyleId.HasValue && styleIds.Contains(ist.StyleId.Value)));

            if (excludeIds != null && excludeIds.Any())
                query = query.Where(x => !excludeIds.Contains(x.Id));

            if (userId.HasValue)
                query = query.Where(x => x.UserId == userId || x.ItemType == ItemType.SYSTEM);

            var items = await query
                .Include(x => x.Category)
                .Include(x => x.User)
                .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                .ToListAsync();

            if (items.Count <= 10)
                return items;

            return items.OrderBy(x => Guid.NewGuid()).Take(10).ToList();
        }
    }
}
