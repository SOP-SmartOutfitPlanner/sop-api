using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    /// <summary>
    /// Outfit management endpoints for creating, viewing, updating, and managing user outfits
    /// </summary>
    [Route("api/v1/outfits")]
    [ApiController]
    [Authorize(Roles = "USER,STYLIST,ADMIN")]
    public class OutfitController : BaseController
    {
        private readonly IOutfitService _outfitService;

        public OutfitController(IOutfitService outfitService)
        {
            _outfitService = outfitService;
        }

        /// <summary>
        /// Get all outfits with pagination and search
        /// </summary>
        /// <remarks>
        /// **Roles:** ADMIN only
        ///
        /// **Query Parameters:**
        /// - `page-index`: Page number (default: 1)
        /// - `page-size`: Items per page (default: 10)
        /// - `q`: Search in name or description (optional)
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> GetAllOutfits([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _outfitService.GetAllOutfitPaginationAsync(paginationParameter));
        }

        /// <summary>
        /// Get user's outfits with pagination, search, and filters
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Query Parameters:**
        /// - `page-index`: Page number (default: 1)
        /// - `page-size`: Items per page (default: 10)
        /// - `q`: Search in name or description (optional)
        /// - `is-favorite`: Filter by favorite status (optional)
        /// - `is-saved`: Filter by saved status (optional)
        /// </remarks>
        [HttpGet("user")]
        public Task<IActionResult> GetOutfitsByUser(
            [FromQuery] PaginationParameter paginationParameter,
            [FromQuery(Name = "is-favorite")] bool? isFavorite,
            [FromQuery(Name = "is-saved")] bool? isSaved)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.GetOutfitByUserPaginationAsync(paginationParameter, userId, isFavorite, isSaved));
        }

        /// <summary>
        /// Get outfit by ID with item details
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Users can only access their own outfits
        /// </remarks>
        [HttpGet("{id}")]
        public Task<IActionResult> GetOutfitById(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.GetOutfitByIdAsync(id, userId));
        }

        /// <summary>
        /// Create a new outfit
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Request Body:**
        /// - `name`: Outfit name (optional)
        /// - `description`: Outfit description (optional)
        /// - `itemIds`: Array of item IDs (optional, prevents duplicates)
        ///
        /// **Note:** UserId is extracted from JWT token automatically
        /// </remarks>
        [HttpPost]
        public Task<IActionResult> CreateOutfit([FromBody] OutfitCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.CreateOutfitAsync(userId, model));
        }

        /// <summary>
        /// Update outfit name and description
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Request Body:**
        /// - `name`: New outfit name (optional)
        /// - `description`: New outfit description (optional)
        ///
        /// **Note:** Users can only update their own outfits
        /// </remarks>
        [HttpPut("{id}")]
        public Task<IActionResult> UpdateOutfit(long id, [FromBody] OutfitUpdateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.UpdateOutfitAsync(id, userId, model));
        }

        /// <summary>
        /// Delete outfit (soft delete)
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Users can only delete their own outfits
        /// </remarks>
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteOutfit(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.DeleteOutfitAsync(id, userId));
        }

        /// <summary>
        /// Toggle outfit favorite status
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        /// </remarks>
        [HttpPut("{id}/favorite")]
        public Task<IActionResult> ToggleOutfitFavorite(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.ToggleOutfitFavoriteAsync(id, userId));
        }

        /// <summary>
        /// Toggle outfit saved status
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        /// </remarks>
        [HttpPut("{id}/save")]
        public Task<IActionResult> ToggleOutfitSave(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.ToggleOutfitSaveAsync(id, userId));
        }

        [HttpPost("suggestion")]
        public Task<IActionResult> OutfitSuggestion()
        {
            throw new NotImplementedException();
        }
    }
}
