using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.FollowerModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/followers")]
    [ApiController]
    public class FollowerController : BaseController
    {
        private readonly IFollowerService _followerService;

        public FollowerController(IFollowerService followerService)
        {
            _followerService = followerService;
        }

        /// <summary>
        /// Follow a user
        /// </summary>
        /// <param name="model">Follower and Following user IDs</param>
        [HttpPost]
        public Task<IActionResult> FollowUser([FromBody] CreateFollowerModel model)
        {
            return ValidateAndExecute(async () => await _followerService.FollowUser(model));
        }

        /// <summary>
        /// Unfollow a user
        /// </summary>
        /// <param name="id">Follower relationship ID</param>
        [HttpDelete("{id}")]
        public Task<IActionResult> UnfollowUser(long id)
        {
            return ValidateAndExecute(async () => await _followerService.UnfollowUser(id));
        }

        /// <summary>
        /// Get follower count for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        [HttpGet("count/followers/{userId}")]
        public Task<IActionResult> GetFollowerCount(long userId)
        {
            return ValidateAndExecute(async () => await _followerService.GetFollowerCount(userId));
        }

        /// <summary>
        /// Check if a user is following another user
        /// </summary>
        /// <param name="followerId">Follower user ID</param>
        /// <param name="followingId">Following user ID</param>
        [HttpGet("status")]
        public Task<IActionResult> IsFollowing([FromQuery] long followerId, [FromQuery] long followingId)
        {
            return ValidateAndExecute(async () => await _followerService.IsFollowing(followerId, followingId));
        }
    }
}
