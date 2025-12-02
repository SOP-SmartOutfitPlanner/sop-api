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
    public class SaveOutfitFromPostRepository : GenericRepository<SaveOutfitFromPost>, ISaveOutfitFromPostRepository
    {
        private readonly SOPServerContext _context;

        public SaveOutfitFromPostRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SaveOutfitFromPost?> GetByUserOutfitAndPostAsync(long userId, long outfitId, long postId)
        {
            return await _context.SaveOutfitFromPosts
                .Include(s => s.Outfit)
                .Include(s => s.Post)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OutfitId == outfitId && s.PostId == postId && !s.IsDeleted);
        }

        public async Task<SaveOutfitFromPost?> GetByUserOutfitAndPostIncludeDeletedAsync(long userId, long outfitId, long postId)
        {
            return await _context.SaveOutfitFromPosts
                .Include(s => s.Outfit)
                .Include(s => s.Post)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OutfitId == outfitId && s.PostId == postId);
        }

        public async Task<bool> ExistsAsync(long userId, long outfitId, long postId)
        {
            return await _context.SaveOutfitFromPosts
                .AnyAsync(s => s.UserId == userId && s.OutfitId == outfitId && s.PostId == postId && !s.IsDeleted);
        }

        public async Task<bool> ExistsByUserAndOutfitAsync(long userId, long outfitId)
        {
            return await _context.SaveOutfitFromPosts
                .AnyAsync(s => s.UserId == userId && s.OutfitId == outfitId && !s.IsDeleted);
        }

        public async Task<IEnumerable<SaveOutfitFromPost>> GetByUserIdAsync(long userId)
        {
            return await _context.SaveOutfitFromPosts
                .Include(s => s.Outfit)
                .Include(s => s.Post)
                    .ThenInclude(p => p.User)
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }

        public IQueryable<SaveOutfitFromPost> GetQueryableByUserId(long userId)
        {
            return _context.SaveOutfitFromPosts
                .Include(s => s.Outfit)
                    .ThenInclude(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemOccasions)
                                .ThenInclude(io => io.Occasion)
                .Include(s => s.Outfit)
                    .ThenInclude(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemSeasons)
                                .ThenInclude(is_ => is_.Season)
                .Include(s => s.Outfit)
                    .ThenInclude(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.ItemStyles)
                                .ThenInclude(ist => ist.Style)
                .Include(s => s.Outfit)
                    .ThenInclude(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                            .ThenInclude(i => i.Category)
                .Include(s => s.Post)
                    .ThenInclude(p => p.User)
                .Where(s => s.UserId == userId && !s.IsDeleted);
        }
    }
}
