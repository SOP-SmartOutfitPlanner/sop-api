using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IOutfitRepository : IGenericRepository<Outfit>
    {
        // Calendar methods
        Task<Pagination<OutfitUsageHistory>> GetOutfitCalendarPaginationAsync(
            PaginationParameter paginationParameter,
            Expression<Func<OutfitUsageHistory, bool>> filter = null,
            Func<IQueryable<OutfitUsageHistory>, IOrderedQueryable<OutfitUsageHistory>> orderBy = null);

        Task<OutfitUsageHistory> GetOutfitCalendarByIdAsync(long id);

        Task<List<OutfitUsageHistory>> GetOutfitCalendarByUserOccasionAsync(long userOccasionId, long userId);

        Task<OutfitUsageHistory> AddOutfitCalendarAsync(OutfitUsageHistory outfitCalendar);

        void UpdateOutfitCalendar(OutfitUsageHistory outfitCalendar);

        void DeleteOutfitCalendar(OutfitUsageHistory outfitCalendar);
    }
}
