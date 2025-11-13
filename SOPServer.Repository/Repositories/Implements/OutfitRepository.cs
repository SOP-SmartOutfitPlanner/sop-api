using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class OutfitRepository : GenericRepository<Outfit>, IOutfitRepository
    {
        private readonly SOPServerContext _context;

        public OutfitRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        // ==================== CALENDAR METHODS ====================

        public async Task<Pagination<OutfitUsageHistory>> GetOutfitCalendarPaginationAsync(
            PaginationParameter paginationParameter,
            Expression<Func<OutfitUsageHistory, bool>> filter = null,
            Func<IQueryable<OutfitUsageHistory>, IOrderedQueryable<OutfitUsageHistory>> orderBy = null)
        {
            IQueryable<OutfitUsageHistory> query = _context.OutfitUsageHistories
                .Include(x => x.User)
                .Include(x => x.Outfit)
                .Include(x => x.UserOccasion)
                .Where(x => !x.IsDeleted);

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((paginationParameter.PageIndex - 1) * paginationParameter.PageSize)
                .Take(paginationParameter.PageSize)
                .ToListAsync();

            return new Pagination<OutfitUsageHistory>(items, totalCount, paginationParameter.PageIndex, paginationParameter.PageSize);
        }

        public async Task<OutfitUsageHistory> GetOutfitCalendarByIdAsync(long id)
        {
            return await _context.OutfitUsageHistories
                .Include(x => x.User)
                .Include(x => x.Outfit)
                    .ThenInclude(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category)
                .Include(x => x.UserOccasion)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<List<OutfitUsageHistory>> GetOutfitCalendarByUserOccasionAsync(long userOccasionId, long userId)
        {
            return await _context.OutfitUsageHistories
                .Include(x => x.User)
                .Include(x => x.Outfit)
                    .ThenInclude(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category)
                .Include(x => x.UserOccasion)
                .Where(x => x.UserOccassionId == userOccasionId && x.UserId == userId && !x.IsDeleted)
                .OrderBy(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<OutfitUsageHistory> AddOutfitCalendarAsync(OutfitUsageHistory outfitCalendar)
        {
            await _context.OutfitUsageHistories.AddAsync(outfitCalendar);
            return outfitCalendar;
        }

        public void UpdateOutfitCalendar(OutfitUsageHistory outfitCalendar)
        {
            outfitCalendar.UpdatedDate = DateTime.UtcNow;
            _context.OutfitUsageHistories.Update(outfitCalendar);
        }

        public void DeleteOutfitCalendar(OutfitUsageHistory outfitCalendar)
        {
            outfitCalendar.IsDeleted = true;
            outfitCalendar.UpdatedDate = DateTime.UtcNow;
            _context.OutfitUsageHistories.Update(outfitCalendar);
        }
    }
}
