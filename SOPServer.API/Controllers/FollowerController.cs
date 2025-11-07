using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
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
        /// Toggle follow/unfollow a user
        /// </summary>
        /// <param name="model">Follower and Following user IDs</param>
        [HttpPost]
        public Task<IActionResult> ToggleFollowUser([FromBody] CreateFollowerModel model)
        {
            return ValidateAndExecute(async () => await _followerService.ToggleFollowUser(model));
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

        /// <summary>
        /// Get list of followers for a specific user with pagination
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters</param>
        /// <param name="userId">User ID</param>
        [HttpGet("followers/{userId}")]
        public Task<IActionResult> GetFollowersByUserId([FromQuery] PaginationParameter paginationParameter, long userId)
        {
            return ValidateAndExecute(async () => await _followerService.GetFollowersByUserId(paginationParameter, userId));
        }

        /// <summary>
        /// Get list of users that a specific user is following with pagination
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters</param>
        /// <param name="userId">User ID</param>
        [HttpGet("following/{userId}")]
        public Task<IActionResult> GetFollowingByUserId([FromQuery] PaginationParameter paginationParameter, long userId)
        {
            return ValidateAndExecute(async () => await _followerService.GetFollowingByUserId(paginationParameter, userId));
        }
    }
}
