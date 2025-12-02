using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class SaveOutfitFromCollectionRepository : GenericRepository<SaveOutfitFromCollection>, ISaveOutfitFromCollectionRepository
    {
        private readonly SOPServerContext _context;

        public SaveOutfitFromCollectionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SaveOutfitFromCollection?> GetByUserOutfitAndCollectionAsync(long userId, long outfitId, long collectionId)
        {
            return await _context.SaveOutfitFromCollections
                .Include(s => s.Outfit)
                .Include(s => s.Collection)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OutfitId == outfitId && s.CollectionId == collectionId && !s.IsDeleted);
        }

        public async Task<SaveOutfitFromCollection?> GetByUserOutfitAndCollectionIncludeDeletedAsync(long userId, long outfitId, long collectionId)
        {
            return await _context.SaveOutfitFromCollections
                .Include(s => s.Outfit)
                .Include(s => s.Collection)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OutfitId == outfitId && s.CollectionId == collectionId);
        }

        public async Task<bool> ExistsAsync(long userId, long outfitId, long collectionId)
        {
            return await _context.SaveOutfitFromCollections
                .AnyAsync(s => s.UserId == userId && s.OutfitId == outfitId && s.CollectionId == collectionId && !s.IsDeleted);
        }

        public async Task<bool> ExistsByUserAndOutfitAsync(long userId, long outfitId)
        {
            return await _context.SaveOutfitFromCollections
                .AnyAsync(s => s.UserId == userId && s.OutfitId == outfitId && !s.IsDeleted);
        }

        public async Task<IEnumerable<SaveOutfitFromCollection>> GetByUserIdAsync(long userId)
        {
            return await _context.SaveOutfitFromCollections
                .Include(s => s.Outfit)
                .Include(s => s.Collection)
                    .ThenInclude(c => c.User)
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }
    }
}
