using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface ICollectionOutfitRepository : IGenericRepository<CollectionOutfit>
    {
        Task<CollectionOutfit?> GetByCollectionAndOutfitAsync(long collectionId, long outfitId);
    }
}
