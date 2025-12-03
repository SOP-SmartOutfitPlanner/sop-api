using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.SaveOutfitFromCollectionModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/outfit-collection")]
    [ApiController]
    [Authorize]
    public class SaveOutfitFromCollectionController : BaseController
    {
        private readonly ISaveOutfitFromCollectionService _service;

        public SaveOutfitFromCollectionController(ISaveOutfitFromCollectionService service)
        {
            _service = service;
        }

        /// <summary>
        /// Save an outfit from a collection to user's wardrobe
        /// </summary>
        /// <param name="model">Outfit ID and Collection ID</param>
        [HttpPost]
        public Task<IActionResult> SaveOutfit([FromBody] SaveOutfitFromCollectionCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.SaveOutfitAsync(long.Parse(userIdClaim), model));
        }

        /// <summary>
        /// Unsave an outfit from a collection
        /// </summary>
        /// <param name="outfitId">Outfit ID</param>
        /// <param name="collectionId">Collection ID</param>
        [HttpDelete("{outfitId}/{collectionId}")]
        public Task<IActionResult> UnsaveOutfit(long outfitId, long collectionId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.UnsaveOutfitAsync(long.Parse(userIdClaim), outfitId, collectionId));
        }

        /// <summary>
        /// Get all saved outfits from collections for the authenticated user
        /// </summary>
        [HttpGet]
        public Task<IActionResult> GetSavedOutfits()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.GetSavedOutfitsByUserAsync(long.Parse(userIdClaim)));
        }

        /// <summary>
        /// Check if an outfit from a collection is saved by the authenticated user
        /// </summary>
        /// <param name="outfitId">Outfit ID</param>
        /// <param name="collectionId">Collection ID</param>
        [HttpGet("check/{outfitId}/{collectionId}")]
        public Task<IActionResult> CheckIfSaved(long outfitId, long collectionId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return ValidateAndExecute(async () =>
                await _service.CheckIfSavedAsync(long.Parse(userIdClaim), outfitId, collectionId));
        }
    }
}
