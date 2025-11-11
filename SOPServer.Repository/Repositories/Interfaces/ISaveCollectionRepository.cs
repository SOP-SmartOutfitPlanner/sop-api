using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface ISaveCollectionRepository : IGenericRepository<SaveCollection>
    {
        Task<SaveCollection?> GetByUserAndCollection(long userId, long collectionId);
    }
}
