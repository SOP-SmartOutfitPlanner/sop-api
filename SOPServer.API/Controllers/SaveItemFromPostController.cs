using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ItemModels;
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
        /// Get all saved items from posts for the authenticated user with pagination, search, and filters
        /// </summary>
        /// <remarks>
        /// **Query Parameters:**
        /// - `page-index`: Page number (default: 1)
        /// - `page-size`: Items per page (default: 10)
        /// - `search`: Search in item name, description, or post body (optional)
        /// - `is-analyzed`: Filter by analysis status (optional)
        /// - `category-id`: Filter by category (supports parent category filtering) (optional)
        /// - `season-id`: Filter by season (optional)
        /// - `style-id`: Filter by style (optional)
        /// - `occasion-id`: Filter by occasion (optional)
        /// - `sort-by-date`: Sort order: 0 = Ascending, 1 = Descending (optional)
        /// </remarks>
        [HttpGet]
        public Task<IActionResult> GetSavedItems([FromQuery] PaginationParameter paginationParameter, [FromQuery] ItemFilterModel filter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.GetSavedItemsByUserAsync(long.Parse(userIdClaim), paginationParameter, filter));
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
