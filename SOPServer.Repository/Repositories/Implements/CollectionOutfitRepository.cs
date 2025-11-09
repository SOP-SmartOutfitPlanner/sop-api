using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class CollectionOutfitRepository : GenericRepository<CollectionOutfit>, ICollectionOutfitRepository
    {
        private readonly SOPServerContext _context;

        public CollectionOutfitRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<CollectionOutfit?> GetByCollectionAndOutfitAsync(long collectionId, long outfitId)
        {
            return await _context.Set<CollectionOutfit>()
                .FirstOrDefaultAsync(co => co.CollectionId == collectionId 
                    && co.OutfitId == outfitId 
                    && !co.IsDeleted);
        }
    }
}
