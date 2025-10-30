using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.LikePostModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/like-posts")]
    [ApiController]
    public class LikePostController : BaseController
    {
        private readonly ILikePostService _likePostService;
        
        public LikePostController(ILikePostService likePostService)
        {
            _likePostService = likePostService;
        }

        /// <summary>
        /// Create a new like on a post
        /// </summary>
        /// <param name="model">Like post creation model</param>
        /// <returns>Created like post</returns>
        [HttpPost]
        public Task<IActionResult> CreateLikePost([FromBody] CreateLikePostModel model)
        {
            return ValidateAndExecute(async () => await _likePostService.CreateLikePost(model));
        }

        /// <summary>
        /// Delete a like from a post (unlike)
        /// </summary>
        /// <param name="id">Like post ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteLikePost(int id)
        {
            return ValidateAndExecute(async () => await _likePostService.DeleteLikePost(id));
        }
    }
}
