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
        public Task<IActionResult> CreatePost([FromForm] PostCreateModel model)
        {
            return ValidateAndExecute(async () => await _postService.CreatePostAsync(model));
        }

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
            return ValidateAndExecute(async () => await _postService.GetPostsByHashtagIdAsync(paginationParameter, hashtagId));
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
