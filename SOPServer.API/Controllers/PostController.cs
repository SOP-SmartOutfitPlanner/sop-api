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
        /// Gets personalized newsfeed for user with Facebook-like refresh dynamics.
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters (pageIndex, pageSize)</param>
        /// <param name="userId">User ID requesting the feed</param>
        /// <param name="sessionId">Optional session ID for seen posts tracking</param>
        [HttpGet("feed")]
        public Task<IActionResult> GetNewsfeed(
            [FromQuery] PaginationParameter paginationParameter,
            [FromQuery] long userId,
            [FromQuery] string? sessionId = null)
        {
            return ValidateAndExecute(async () => 
                await _postService.GetNewsFeedAsync(paginationParameter, userId, sessionId));
        }
    }
}
