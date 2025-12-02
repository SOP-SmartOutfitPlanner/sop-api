using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface ISaveItemFromPostRepository : IGenericRepository<SaveItemFromPost>
    {
        Task<SaveItemFromPost?> GetByUserItemAndPostAsync(long userId, long itemId, long postId);
        Task<SaveItemFromPost?> GetByUserItemAndPostIncludeDeletedAsync(long userId, long itemId, long postId);
        Task<bool> ExistsAsync(long userId, long itemId, long postId);
        Task<bool> ExistsByUserAndItemAsync(long userId, long itemId);
        Task<IEnumerable<SaveItemFromPost>> GetByUserIdAsync(long userId);
    }
}
