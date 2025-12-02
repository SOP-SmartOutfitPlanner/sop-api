using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface ISaveOutfitFromCollectionRepository : IGenericRepository<SaveOutfitFromCollection>
    {
        Task<SaveOutfitFromCollection?> GetByUserOutfitAndCollectionAsync(long userId, long outfitId, long collectionId);
        Task<SaveOutfitFromCollection?> GetByUserOutfitAndCollectionIncludeDeletedAsync(long userId, long outfitId, long collectionId);
        Task<bool> ExistsAsync(long userId, long outfitId, long collectionId);
        Task<bool> ExistsByUserAndOutfitAsync(long userId, long outfitId);
        Task<IEnumerable<SaveOutfitFromCollection>> GetByUserIdAsync(long userId);
    }
}
