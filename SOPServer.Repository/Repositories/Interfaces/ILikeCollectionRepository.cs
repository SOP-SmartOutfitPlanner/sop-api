using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface ILikeCollectionRepository : IGenericRepository<LikeCollection>
    {
        Task<LikeCollection?> GetByUserAndCollection(long userId, long collectionId);
    }
}
