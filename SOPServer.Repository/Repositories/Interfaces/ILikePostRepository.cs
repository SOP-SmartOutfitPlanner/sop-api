using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface ILikePostRepository : IGenericRepository<LikePost>
    {
        Task<LikePost?> GetByUserAndPost(long userId, long postId);
    }
}
