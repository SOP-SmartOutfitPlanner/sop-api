using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.SaveOutfitFromPostModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/outfit-post")]
    [ApiController]
    [Authorize]
    public class SaveOutfitFromPostController : BaseController
    {
        private readonly ISaveOutfitFromPostService _service;

        public SaveOutfitFromPostController(ISaveOutfitFromPostService service)
        {
            _service = service;
        }

        /// <summary>
        /// Save an outfit from a post to user's wardrobe
        /// </summary>
        /// <param name="model">Outfit ID and Post ID</param>
        [HttpPost]
        public Task<IActionResult> SaveOutfit([FromBody] SaveOutfitFromPostCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.SaveOutfitAsync(long.Parse(userIdClaim), model));
        }

        /// <summary>
        /// Unsave an outfit from a post
        /// </summary>
        /// <param name="outfitId">Outfit ID</param>
        /// <param name="postId">Post ID</param>
        [HttpDelete("{outfitId}/{postId}")]
        public Task<IActionResult> UnsaveOutfit(long outfitId, long postId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.UnsaveOutfitAsync(long.Parse(userIdClaim), outfitId, postId));
        }

        /// <summary>
        /// Get all saved outfits from posts for the authenticated user with pagination and search
        /// </summary>
        /// <remarks>
        /// **Query Parameters:**
        /// - `page-index`: Page number (default: 1)
        /// - `page-size`: Items per page (default: 10)
        /// - `search`: Search in outfit name, description, or post body (optional)
        /// </remarks>
        [HttpGet]
        public Task<IActionResult> GetSavedOutfits([FromQuery] PaginationParameter paginationParameter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.GetSavedOutfitsByUserAsync(long.Parse(userIdClaim), paginationParameter));
        }

        /// <summary>
        /// Check if an outfit from a post is saved by the authenticated user
        /// </summary>
        /// <param name="outfitId">Outfit ID</param>
        /// <param name="postId">Post ID</param>
        [HttpGet("check/{outfitId}/{postId}")]
        public Task<IActionResult> CheckIfSaved(long outfitId, long postId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.CheckIfSavedAsync(long.Parse(userIdClaim), outfitId, postId));
        }
    }
}
