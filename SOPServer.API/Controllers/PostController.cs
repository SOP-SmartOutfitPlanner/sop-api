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
            return ValidateAndExecute(async () => await _postService.GetPostByUserIdAsync(paginationParameter, userId));
        }

        [HttpGet("hashtag/{hashtagId}")]
        public Task<IActionResult> GetPostsByHashtagId(
            [FromQuery] PaginationParameter paginationParameter,
            long hashtagId)
        {
            return ValidateAndExecute(async () => await _postService.GetPostsByHashtagIdAsync(paginationParameter, hashtagId));
        }

        [HttpGet]
        public Task<IActionResult> GetAllPosts(PaginationParameter paginationParameter, long userId)
        {
            return ValidateAndExecute(async () => await _postService.GetAllPostsAsync(paginationParameter, userId));
        }
    }
}
