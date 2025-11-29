using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/posts")]
    [ApiController]
    public class PostController : BaseController
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        [HttpGet("{id}")]
        public Task<IActionResult> GetPostById(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? requesterId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                requesterId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _postService.GetPostByIdAsync(id, requesterId));
        }

        /// <summary>
        /// Creates a new post
        /// </summary>
        /// <param name="model">The post creation model</param>
        /// <returns>The created post</returns>
        /// <remarks>
        /// ItemIds (0-4 items) and OutfitId (0-1 outfit) are mutually exclusive. Cannot provide both.
        /// - To add items: provide ItemIds only (max 4 items)
        /// - To add outfit: provide OutfitId only
        /// - Both fields are optional
        /// </remarks>
        [HttpPost]
        public Task<IActionResult> CreatePost([FromForm] PostCreateModel model)
        {
            return ValidateAndExecute(async () => await _postService.CreatePostAsync(model));
        }

        /// <summary>
        /// Updates an existing post
        /// </summary>
        /// <param name="id">The post ID</param>
        /// <param name="model">The post update model</param>
        /// <returns>The updated post</returns>
        /// <remarks>
        /// ItemIds (0-4 items) and OutfitId (0-1 outfit) are mutually exclusive. Cannot provide both.
        /// - To add items: provide ItemIds only (max 4 items)
        /// - To add outfit: provide OutfitId only
        /// - Both fields are optional
        /// - Null value means clear all associations (items or outfit)
        /// </remarks>
        [HttpPut("{id}")]
        public Task<IActionResult> UpdatePost(long id, [FromForm] PostUpdateModel model)
        {
            return ValidateAndExecute(async () => await _postService.UpdatePostAsync(id, model));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeletePost(long id)
        {
            return ValidateAndExecute(async () => await _postService.DeletePostByIdAsync(id));
        }

        [HttpGet("user/{userId}")]
        public Task<IActionResult> GetPostByUserId(
            [FromQuery] PaginationParameter paginationParameter,
            long userId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? callerUserId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                callerUserId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _postService.GetPostByUserIdAsync(paginationParameter, userId, callerUserId));
        }

        [HttpGet("hashtag/{hashtagId}")]
        public Task<IActionResult> GetPostsByHashtagId(
            [FromQuery] PaginationParameter paginationParameter,
            long hashtagId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? requesterId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                requesterId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _postService.GetPostsByHashtagIdAsync(paginationParameter, hashtagId, requesterId));
        }

        [HttpGet("hashtag/name/{hashtagName}")]
        public Task<IActionResult> GetPostsByHashtagName(
            [FromQuery] PaginationParameter paginationParameter,
            string hashtagName)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? callerUserId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                callerUserId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _postService.GetPostsByHashtagNameAsync(paginationParameter, hashtagName, callerUserId));
        }

        [HttpGet]
        public Task<IActionResult> GetAllPosts(PaginationParameter paginationParameter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? callerUserId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                callerUserId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _postService.GetAllPostsAsync(paginationParameter, callerUserId));
        }

        [HttpGet("top-contributors")]
        public Task<IActionResult> GetTopContributors([FromQuery] PaginationParameter paginationParameter, [FromQuery] long? userId = null)
        {
            return ValidateAndExecute(async () => await _postService.GetTopContributorsAsync(paginationParameter, userId));
        }

        [HttpGet("{postId}/likers")]
        public Task<IActionResult> GetPostLikers(
            [FromQuery] PaginationParameter paginationParameter,
            long postId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? callerUserId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                callerUserId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _postService.GetPostLikersAsync(paginationParameter, postId, callerUserId));
        }
    }
}
