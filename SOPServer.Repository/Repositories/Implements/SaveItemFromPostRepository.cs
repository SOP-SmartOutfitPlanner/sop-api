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
    public class SaveItemFromPostRepository : GenericRepository<SaveItemFromPost>, ISaveItemFromPostRepository
    {
        private readonly SOPServerContext _context;

        public SaveItemFromPostRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SaveItemFromPost?> GetByUserItemAndPostAsync(long userId, long itemId, long postId)
        {
            return await _context.SaveItemFromPosts
                .Include(s => s.Item)
                .Include(s => s.Post)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ItemId == itemId && s.PostId == postId && !s.IsDeleted);
        }

        public async Task<SaveItemFromPost?> GetByUserItemAndPostIncludeDeletedAsync(long userId, long itemId, long postId)
        {
            return await _context.SaveItemFromPosts
                .Include(s => s.Item)
                .Include(s => s.Post)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.ItemId == itemId && s.PostId == postId);
        }

        public async Task<bool> ExistsAsync(long userId, long itemId, long postId)
        {
            return await _context.SaveItemFromPosts
                .AnyAsync(s => s.UserId == userId && s.ItemId == itemId && s.PostId == postId && !s.IsDeleted);
        }

        public async Task<bool> ExistsByUserAndItemAsync(long userId, long itemId)
        {
            return await _context.SaveItemFromPosts
                .AnyAsync(s => s.UserId == userId && s.ItemId == itemId && !s.IsDeleted);
        }

        public async Task<IEnumerable<SaveItemFromPost>> GetByUserIdAsync(long userId)
        {
            return await _context.SaveItemFromPosts
                .Include(s => s.Item)
                .Include(s => s.Post)
                    .ThenInclude(p => p.User)
                .Where(s => s.UserId == userId && !s.IsDeleted)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
        }
    }
}
