using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IFollowerRepository : IGenericRepository<Follower>
    {
        Task<Follower?> GetByFollowerAndFollowing(long followerId, long followingId);
        Task<Follower?> GetByFollowerAndFollowingIncludeDeleted(long followerId, long followingId);
        Task<int> GetFollowerCount(long userId);
        Task<bool> IsFollowing(long followerId, long followingId);
    }
}
