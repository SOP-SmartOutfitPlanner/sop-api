using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/outfits")]
    [ApiController]
    public class OutfitController : BaseController
    {
        private readonly IOutfitService _outfitService;

        public OutfitController(IOutfitService outfitService)
        {
            _outfitService = outfitService;
        }

        /// <summary>
        /// Get outfit by ID
        /// </summary>
        [HttpGet("{id}")]
        public Task<IActionResult> GetOutfitById(long id)
        {
            return ValidateAndExecute(async () => await _outfitService.GetOutfitByIdAsync(id));
        }

        /// <summary>
        /// Toggle outfit favorite status
        /// </summary>
        [HttpPatch("{id}/favorite")]
        public Task<IActionResult> ToggleOutfitFavorite(long id)
        {
            return ValidateAndExecute(async () => await _outfitService.ToggleOutfitFavoriteAsync(id));
        }

        /// <summary>
        /// Set outfit used status to true
        /// </summary>
        [HttpPatch("{id}/used")]
        public Task<IActionResult> MarkOutfitAsUsed(long id)
        {
            return ValidateAndExecute(async () => await _outfitService.MarkOutfitAsUsedAsync(id));
        }
    }
}
