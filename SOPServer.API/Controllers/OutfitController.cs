using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/outfits")]
    [ApiController]
    [Authorize(Roles = "USER,STYLIST")]
    public class OutfitController : BaseController
    {
        private readonly IOutfitService _outfitService;

        public OutfitController(IOutfitService outfitService)
        {
            _outfitService = outfitService;
        }

        /// <summary>
        /// Get all outfits with pagination and search (admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> GetAllOutfits([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _outfitService.GetAllOutfitPaginationAsync(paginationParameter));
        }

        /// <summary>
        /// Get outfits by user with pagination and search (from authenticated user)
        /// </summary>
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
        /// Get outfit by ID
        /// </summary>
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
        [HttpPost]
        public Task<IActionResult> CreateOutfit([FromBody] OutfitCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.CreateOutfitAsync(userId, model));
        }

        /// <summary>
        /// Update an outfit
        /// </summary>
        [HttpPut("{id}")]
        public Task<IActionResult> UpdateOutfit(long id, [FromBody] OutfitUpdateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.UpdateOutfitAsync(id, userId, model));
        }

        /// <summary>
        /// Delete an outfit
        /// </summary>
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
        [HttpPut("{id}/favorite")]
        public Task<IActionResult> ToggleOutfitFavorite(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.ToggleOutfitFavoriteAsync(id, userId));
        }

        /// <summary>
        /// Toggle outfit save status
        /// </summary>
        [HttpPut("{id}/save")]
        public Task<IActionResult> ToggleOutfitSave(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _outfitService.ToggleOutfitSaveAsync(id, userId));
        }
    }
}
