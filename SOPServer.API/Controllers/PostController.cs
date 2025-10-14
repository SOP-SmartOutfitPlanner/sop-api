using Microsoft.AspNetCore.Mvc;
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
    }
}
