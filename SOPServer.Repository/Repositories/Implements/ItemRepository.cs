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

        public async Task<int> CountAnalyzedItemByUserId(long userId)
        {
            return await _context.Items.CountAsync(x => !x.IsDeleted && x.UserId == userId && x.IsAnalyzed == true);
        }

        public async Task<int> CountSystemItems()
        {
            return await _context.Items.CountAsync(x => !x.IsDeleted && x.ItemType == ItemType.SYSTEM);
        }

        public async Task<int> CountAnalyzedSystemItems()
        {
            return await _context.Items.CountAsync(x => !x.IsDeleted && x.ItemType == ItemType.SYSTEM && x.IsAnalyzed == true);
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

            var baseQuery = _context.Items
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true &&
                           x.ItemSeasons.Any(ise => !ise.IsDeleted && ise.SeasonId.HasValue && seasonIds.Contains(ise.SeasonId.Value)));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Get user items (10 items)
                var userItems = await baseQuery
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                var userItemsToTake = Math.Min(10, userItems.Count);
                if (userItems.Count > 0)
                {
                    items.AddRange(userItems.OrderBy(x => Guid.NewGuid()).Take(userItemsToTake));
                }

                // Get system items to fill remaining slots (5 items + any shortfall from user items)
                var systemItemsNeeded = 15 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !items.Select(i => i.Id).Contains(x.Id))
                        .ToListAsync();

                    if (systemItems.Count > 0)
                    {
                        items.AddRange(systemItems.OrderBy(x => Guid.NewGuid()).Take(systemItemsNeeded));
                    }
                }
            }
            else
            {
                // If no userId provided, get all available items
                items = await baseQuery
                    .OrderBy(x => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();
            }

            return items.Take(15).ToList();
        }

        public async Task<List<Item>> GetItemsByOccasionIdsAsync(List<long> occasionIds, long? userId = null)
        {
            if (occasionIds == null || !occasionIds.Any())
                return new List<Item>();

            var baseQuery = _context.Items
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true &&
                           x.ItemOccasions.Any(io => !io.IsDeleted && io.OccasionId.HasValue && occasionIds.Contains(io.OccasionId.Value)));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Get user items (10 items)
                var userItems = await baseQuery
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                var userItemsToTake = Math.Min(10, userItems.Count);
                if (userItems.Count > 0)
                {
                    items.AddRange(userItems.OrderBy(x => Guid.NewGuid()).Take(userItemsToTake));
                }

                // Get system items to fill remaining slots (5 items + any shortfall from user items)
                var systemItemsNeeded = 15 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !items.Select(i => i.Id).Contains(x.Id))
                        .ToListAsync();

                    if (systemItems.Count > 0)
                    {
                        items.AddRange(systemItems.OrderBy(x => Guid.NewGuid()).Take(systemItemsNeeded));
                    }
                }
            }
            else
            {
                // If no userId provided, get all available items
                items = await baseQuery
                    .OrderBy(x => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();
            }

            return items.Take(15).ToList();
        }

        public async Task<List<Item>> GetItemsByStyleIdsAsync(List<long> styleIds, long? userId = null)
        {
            if (styleIds == null || !styleIds.Any())
                return new List<Item>();

            var baseQuery = _context.Items
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true &&
                           x.ItemStyles.Any(ist => !ist.IsDeleted && ist.StyleId.HasValue && styleIds.Contains(ist.StyleId.Value)));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Get user items (10 items)
                var userItems = await baseQuery
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                var userItemsToTake = Math.Min(10, userItems.Count);
                if (userItems.Count > 0)
                {
                    items.AddRange(userItems.OrderBy(x => Guid.NewGuid()).Take(userItemsToTake));
                }

                // Get system items to fill remaining slots (5 items + any shortfall from user items)
                var systemItemsNeeded = 15 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !items.Select(i => i.Id).Contains(x.Id))
                        .ToListAsync();

                    if (systemItems.Count > 0)
                    {
                        items.AddRange(systemItems.OrderBy(x => Guid.NewGuid()).Take(systemItemsNeeded));
                    }
                }
            }
            else
            {
                // If no userId provided, get all available items
                items = await baseQuery
                    .OrderBy(x => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();
            }

            return items.Take(15).ToList();
        }

        public async Task<List<Item>> GetItemsBySeasonIdsAsync(List<long> seasonIds, List<long> excludeIds, long? userId = null)
        {
            if (seasonIds == null || !seasonIds.Any())
                return new List<Item>();

            var baseQuery = _context.Items
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true &&
                           x.ItemSeasons.Any(ise => !ise.IsDeleted && ise.SeasonId.HasValue && seasonIds.Contains(ise.SeasonId.Value)));

            if (excludeIds != null && excludeIds.Any())
                baseQuery = baseQuery.Where(x => !excludeIds.Contains(x.Id));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Get user items (60% = 6 items)
                var userItems = await baseQuery
                    .Where(x => x.UserId == userId)
                    .Include(x => x.Category)
                    .Include(x => x.User)
                    .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                    .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                    .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                    .ToListAsync();

                var userItemsToTake = Math.Min(8, userItems.Count);
                if (userItems.Count > 0)
                {
                    items.AddRange(userItems.OrderBy(x => Guid.NewGuid()).Take(userItemsToTake));
                }

                // Get system items to fill remaining slots (4 items + any shortfall from user items)
                var systemItemsNeeded = 10 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !items.Select(i => i.Id).Contains(x.Id))
                        .Include(x => x.Category)
                        .Include(x => x.User)
                        .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                        .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                        .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                        .ToListAsync();

                    if (systemItems.Count > 0)
                    {
                        items.AddRange(systemItems.OrderBy(x => Guid.NewGuid()).Take(systemItemsNeeded));
                    }
                }
            }
            else
            {
                // If no userId provided, get all available items
                items = await baseQuery
                    .Include(x => x.Category)
                    .Include(x => x.User)
                    .Include(x => x.ItemOccasions).ThenInclude(x => x.Occasion)
                    .Include(x => x.ItemSeasons).ThenInclude(x => x.Season)
                    .Include(x => x.ItemStyles).ThenInclude(x => x.Style)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(10)
                    .ToListAsync();
            }

            return items.Take(10).ToList();
        }

        public async Task<List<Item>> GetItemsByOccasionIdsAsync(List<long> occasionIds, List<long> excludeIds, long? userId = null)
        {
            if (occasionIds == null || !occasionIds.Any())
                return new List<Item>();

            var baseQuery = _context.Items
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true && 
                           x.ItemOccasions.Any(io => !io.IsDeleted && io.OccasionId.HasValue && occasionIds.Contains(io.OccasionId.Value)));

            if (excludeIds != null && excludeIds.Any())
                baseQuery = baseQuery.Where(x => !excludeIds.Contains(x.Id));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Get user items (60% = 6 items)
                var userItems = await baseQuery
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                var userItemsToTake = Math.Min(10, userItems.Count);
                if (userItems.Count > 0)
                {
                    items.AddRange(userItems.OrderBy(x => Guid.NewGuid()).Take(userItemsToTake));
                }

                // Get system items to fill remaining slots (4 items + any shortfall from user items)
                var systemItemsNeeded = 15 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !items.Select(i => i.Id).Contains(x.Id))
                        .ToListAsync();

                    if (systemItems.Count > 0)
                    {
                        items.AddRange(systemItems.OrderBy(x => Guid.NewGuid()).Take(systemItemsNeeded));
                    }
                }
            }
            else
            {
                // If no userId provided, get all available items
                items = await baseQuery
                    .OrderBy(x => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();
            }

            return items.Take(15).ToList();
        }

        public async Task<List<Item>> GetItemsByStyleIdsAsync(List<long> styleIds, List<long> excludeIds, long? userId = null)
        {
            if (styleIds == null || !styleIds.Any())
                return new List<Item>();

            var baseQuery = _context.Items
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true && 
                           x.ItemStyles.Any(ist => !ist.IsDeleted && ist.StyleId.HasValue && styleIds.Contains(ist.StyleId.Value)));

            if (excludeIds != null && excludeIds.Any())
                baseQuery = baseQuery.Where(x => !excludeIds.Contains(x.Id));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Get user items (10 items)
                var userItems = await baseQuery
                    .Where(x => x.UserId == userId)
                    .ToListAsync();

                var userItemsToTake = Math.Min(10, userItems.Count);
                if (userItems.Count > 0)
                {
                    items.AddRange(userItems.OrderBy(x => Guid.NewGuid()).Take(userItemsToTake));
                }

                // Get system items to fill remaining slots (5 items + any shortfall from userItems)
                var systemItemsNeeded = 15 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !items.Select(i => i.Id).Contains(x.Id))
                        .ToListAsync();

                    if (systemItems.Count > 0)
                    {
                        items.AddRange(systemItems.OrderBy(x => Guid.NewGuid()).Take(systemItemsNeeded));
                    }
                }
            }
            else
            {
                // If no userId provided, get all available items
                items = await baseQuery
                    .OrderBy(x => Guid.NewGuid())
                    .Take(15)
                    .ToListAsync();
            }

            return items.Take(15).ToList();
        }

        public async Task<List<Item>> GetItemsBySeasonIdsAsync(List<long> seasonIds, List<long> excludeIds, long? userId, int? gapDay, DateTime? targetDate)
        {
            if (seasonIds == null || !seasonIds.Any())
                return new List<Item>();

            var baseQuery = _context.Items.Include(x => x.ItemSeasons)
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true &&
                           x.ItemSeasons.Any(ise => !ise.IsDeleted && ise.SeasonId.HasValue && seasonIds.Contains(ise.SeasonId.Value)));

            if (excludeIds != null && excludeIds.Any())
                baseQuery = baseQuery.Where(x => !excludeIds.Contains(x.Id));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Build user items query with gapDay filter
                var userItemsQuery = baseQuery.Where(x => x.UserId == userId);
                
                // Apply gapDay filter ONLY to user items (not system items)
                if (gapDay.HasValue && gapDay.Value > 0)
                {
                    var referenceDate = targetDate?.Date ?? DateTime.Today;
                    var startDate = referenceDate.AddDays(-gapDay.Value);
                    var endDate = referenceDate.AddDays(gapDay.Value);

                    // Exclude user items that have been worn within the gap day range
                    // This translates to SQL NOT EXISTS - efficient on database
                    userItemsQuery = userItemsQuery.Where(x => 
                        !_context.ItemWornAtHistories.Any(w => 
                            !w.IsDeleted && 
                            w.ItemId == x.Id && 
                            w.WornAt >= startDate && 
                            w.WornAt <= endDate
                        )
                    );
                }

                // Get up to 15 user items with database-level random ordering
                // Use EF.Functions.Random() for SQL-level randomization to avoid loading all items into memory
                var userItems = await userItemsQuery
                    .OrderBy(x => EF.Functions.Random())
                    .Take(15)
                    .ToListAsync();

                items.AddRange(userItems);

                // Get system items only to fill remaining slots (if user items < 15)
                // System items are NOT affected by gapDay filter
                var systemItemsNeeded = 15 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var takenIds = items.Select(i => i.Id).ToList();
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !takenIds.Contains(x.Id))
                        .OrderBy(x => EF.Functions.Random())
                        .Take(systemItemsNeeded)
                        .ToListAsync();

                    items.AddRange(systemItems);
                }
            }
            else
            {
                // If no userId provided, get all available items with database-level random
                items = await baseQuery
                    .OrderBy(x => EF.Functions.Random())
                    .Take(15)
                    .ToListAsync();
            }

            return items;
        }

        public async Task<List<Item>> GetItemsByOccasionIdsAsync(List<long> occasionIds, List<long> excludeIds, long? userId, int? gapDay, DateTime? targetDate)
        {
            if (occasionIds == null || !occasionIds.Any())
                return new List<Item>();

            var baseQuery = _context.Items.Include(x => x.ItemOccasions)
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true && 
                           x.ItemOccasions.Any(io => !io.IsDeleted && io.OccasionId.HasValue && occasionIds.Contains(io.OccasionId.Value)));

            if (excludeIds != null && excludeIds.Any())
                baseQuery = baseQuery.Where(x => !excludeIds.Contains(x.Id));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Build user items query with gapDay filter
                var userItemsQuery = baseQuery.Where(x => x.UserId == userId);
                
                // Apply gapDay filter ONLY to user items (not system items)
                if (gapDay.HasValue && gapDay.Value > 0)
                {
                    var referenceDate = targetDate?.Date ?? DateTime.Today;
                    var startDate = referenceDate.AddDays(-gapDay.Value);
                    var endDate = referenceDate.AddDays(gapDay.Value);

                    // Exclude user items that have been worn within the gap day range
                    // This translates to SQL NOT EXISTS - efficient on database
                    userItemsQuery = userItemsQuery.Where(x => 
                        !_context.ItemWornAtHistories.Any(w => 
                            !w.IsDeleted && 
                            w.ItemId == x.Id && 
                            w.WornAt >= startDate && 
                            w.WornAt <= endDate
                        )
                    );
                }

                // Get up to 15 user items with database-level random ordering
                // Use EF.Functions.Random() for SQL-level randomization to avoid loading all items into memory
                var userItems = await userItemsQuery
                    .OrderBy(x => EF.Functions.Random())
                    .Take(15)
                    .ToListAsync();

                items.AddRange(userItems);

                // Get system items only to fill remaining slots (if user items < 15)
                // System items are NOT affected by gapDay filter
                var systemItemsNeeded = 15 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var takenIds = items.Select(i => i.Id).ToList();
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !takenIds.Contains(x.Id))
                        .OrderBy(x => EF.Functions.Random())
                        .Take(systemItemsNeeded)
                        .ToListAsync();

                    items.AddRange(systemItems);
                }
            }
            else
            {
                // If no userId provided, get all available items with database-level random
                items = await baseQuery
                    .OrderBy(x => EF.Functions.Random())
                    .Take(15)
                    .ToListAsync();
            }

            return items;
        }

        public async Task<List<Item>> GetItemsByStyleIdsAsync(List<long> styleIds, List<long> excludeIds, long? userId, int? gapDay, DateTime? targetDate)
        {
            if (styleIds == null || !styleIds.Any())
                return new List<Item>();

            var baseQuery = _context.Items.Include(x => x.ItemStyles)
                .Where(x => !x.IsDeleted && x.IsAnalyzed == true && 
                           x.ItemStyles.Any(ist => !ist.IsDeleted && ist.StyleId.HasValue && styleIds.Contains(ist.StyleId.Value)));

            if (excludeIds != null && excludeIds.Any())
                baseQuery = baseQuery.Where(x => !excludeIds.Contains(x.Id));

            var items = new List<Item>();

            if (userId.HasValue)
            {
                // Build user items query with gapDay filter
                var userItemsQuery = baseQuery.Where(x => x.UserId == userId);
                
                // Apply gapDay filter ONLY to user items (not system items)
                if (gapDay.HasValue && gapDay.Value > 0)
                {
                    var referenceDate = targetDate?.Date ?? DateTime.Today;
                    var startDate = referenceDate.AddDays(-gapDay.Value);
                    var endDate = referenceDate.AddDays(gapDay.Value);

                    // Exclude user items that have been worn within the gap day range
                    // This translates to SQL NOT EXISTS - efficient on database
                    userItemsQuery = userItemsQuery.Where(x => 
                        !_context.ItemWornAtHistories.Any(w => 
                            !w.IsDeleted && 
                            w.ItemId == x.Id && 
                            w.WornAt >= startDate && 
                            w.WornAt <= endDate
                        )
                    );
                }

                // Get up to 15 user items with database-level random ordering
                // Use EF.Functions.Random() for SQL-level randomization to avoid loading all items into memory
                var userItems = await userItemsQuery
                    .OrderBy(x => EF.Functions.Random())
                    .Take(15)
                    .ToListAsync();

                items.AddRange(userItems);

                // Get system items only to fill remaining slots (if user items < 15)
                // System items are NOT affected by gapDay filter
                var systemItemsNeeded = 15 - items.Count;
                if (systemItemsNeeded > 0)
                {
                    var takenIds = items.Select(i => i.Id).ToList();
                    var systemItems = await baseQuery
                        .Where(x => x.ItemType == ItemType.SYSTEM && !takenIds.Contains(x.Id))
                        .OrderBy(x => EF.Functions.Random())
                        .Take(systemItemsNeeded)
                        .ToListAsync();

                    items.AddRange(systemItems);
                }
            }
            else
            {
                // If no userId provided, get all available items with database-level random
                items = await baseQuery
                    .OrderBy(x => EF.Functions.Random())
                    .Take(15)
                    .ToListAsync();
            }

            return items;
        }
    }
}
