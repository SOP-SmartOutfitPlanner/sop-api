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
            return ValidateAndExecute(async () => await _postService.GetPostByIdAsync(id));
        }

        [HttpPost]
        public Task<IActionResult> CreatePost([FromBody] PostCreateModel model)
        {
            return ValidateAndExecute(async () => await _postService.CreatePostAsync(model));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeletePost(long id)
        {
            return ValidateAndExecute(async () => await _postService.DeletePostByIdAsync(id));
        }

        /// <summary>
        /// Gets personalized newsfeed for user with simple ranking algorithm.
        /// Posts are ranked by recency (40%) and engagement (60%).
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters (pageIndex, pageSize)</param>
        /// <param name="userId">User ID requesting the feed</param>
        [HttpGet("feed")]
        public Task<IActionResult> GetNewsfeed(
            [FromQuery] PaginationParameter paginationParameter,
            [FromQuery] long userId,
            [FromQuery] string? sessionId)
        {
            return ValidateAndExecute(async () => 
                await _postService.GetNewsFeedAsync(paginationParameter, userId, sessionId));
        }

        /// <summary>
        /// Gets all posts by a specific user with pagination.
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters (pageIndex, pageSize)</param>
        /// <param name="userId">User ID whose posts to retrieve</param>
        [HttpGet("user/{userId}")]
        public Task<IActionResult> GetPostByUserId(
            [FromQuery] PaginationParameter paginationParameter, 
            long userId)
        {
            return ValidateAndExecute(async () => await _postService.GetPostByUserIdAsync(paginationParameter, userId));
        }

        [HttpGet]
        public Task<IActionResult> GetAllPosts(PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _postService.GetAllPostsAsync(paginationParameter));
        }
    }
}
