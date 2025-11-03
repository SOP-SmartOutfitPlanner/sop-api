using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class FollowerRepository : GenericRepository<Follower>, IFollowerRepository
    {
        private readonly SOPServerContext _context;

        public FollowerRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Follower?> GetByFollowerAndFollowing(long followerId, long followingId)
        {
            return await _context.Followers
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId && !f.IsDeleted);
        }

        public async Task<int> GetFollowerCount(long userId)
        {
            return await _context.Followers
                .CountAsync(f => f.FollowingId == userId && !f.IsDeleted);
        }

        public async Task<bool> IsFollowing(long followerId, long followingId)
        {
            return await _context.Followers
                .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId && !f.IsDeleted);
        }
    }
}
