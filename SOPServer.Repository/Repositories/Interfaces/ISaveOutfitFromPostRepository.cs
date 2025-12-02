using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface ISaveOutfitFromPostRepository : IGenericRepository<SaveOutfitFromPost>
    {
        Task<SaveOutfitFromPost?> GetByUserOutfitAndPostAsync(long userId, long outfitId, long postId);
        Task<SaveOutfitFromPost?> GetByUserOutfitAndPostIncludeDeletedAsync(long userId, long outfitId, long postId);
        Task<bool> ExistsAsync(long userId, long outfitId, long postId);
        Task<bool> ExistsByUserAndOutfitAsync(long userId, long outfitId);
        Task<IEnumerable<SaveOutfitFromPost>> GetByUserIdAsync(long userId);
        IQueryable<SaveOutfitFromPost> GetQueryableByUserId(long userId);
    }
}
