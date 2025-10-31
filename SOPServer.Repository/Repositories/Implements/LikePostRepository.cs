using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class LikePostRepository : GenericRepository<LikePost>, ILikePostRepository
    {
        private readonly SOPServerContext _context;

        public LikePostRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<LikePost?> GetByUserAndPost(long userId, long postId)
        {
            return await _context.LikePosts
                .FirstOrDefaultAsync(lp => lp.UserId == userId && lp.PostId == postId && !lp.IsDeleted);
        }
    }
}
