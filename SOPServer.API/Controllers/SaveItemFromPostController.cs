using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.SaveItemFromPostModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/item-post")]
    [ApiController]
    [Authorize]
    public class SaveItemFromPostController : BaseController
    {
        private readonly ISaveItemFromPostService _service;

        public SaveItemFromPostController(ISaveItemFromPostService service)
        {
            _service = service;
        }

        /// <summary>
        /// Save an item from a post to user's wardrobe
        /// </summary>
        /// <param name="model">Item ID and Post ID</param>
        [HttpPost]
        public Task<IActionResult> SaveItem([FromBody] SaveItemFromPostCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.SaveItemAsync(long.Parse(userIdClaim), model));
        }

        /// <summary>
        /// Unsave an item from a post
        /// </summary>
        /// <param name="itemId">Item ID</param>
        /// <param name="postId">Post ID</param>
        [HttpDelete("{itemId}/{postId}")]
        public Task<IActionResult> UnsaveItem(long itemId, long postId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.UnsaveItemAsync(long.Parse(userIdClaim), itemId, postId));
        }

        /// <summary>
        /// Get all saved items from posts for the authenticated user
        /// </summary>
        [HttpGet]
        public Task<IActionResult> GetSavedItems()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.GetSavedItemsByUserAsync(long.Parse(userIdClaim)));
        }

        /// <summary>
        /// Check if an item from a post is saved by the authenticated user
        /// </summary>
        /// <param name="itemId">Item ID</param>
        /// <param name="postId">Post ID</param>
        [HttpGet("check/{itemId}/{postId}")]
        public Task<IActionResult> CheckIfSaved(long itemId, long postId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.CheckIfSavedAsync(long.Parse(userIdClaim), itemId, postId));
        }
    }
}
