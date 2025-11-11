using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.SaveCollectionModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/save-collections")]
    [ApiController]
    public class SaveCollectionController : BaseController
    {
        private readonly ISaveCollectionService _saveCollectionService;

        public SaveCollectionController(ISaveCollectionService saveCollectionService)
        {
            _saveCollectionService = saveCollectionService;
        }

        /// <summary>
        /// Toggle save/unsave a collection
        /// </summary>
        /// <param name="model">Collection ID and User ID</param>
        [HttpPost]
        public Task<IActionResult> ToggleSaveCollection([FromBody] CreateSaveCollectionModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                model.UserId = userId;
            }

            return ValidateAndExecute(async () => await _saveCollectionService.ToggleSaveCollectionAsync(model));
        }

        /// <summary>
        /// Get all saved collections for a specific user with pagination
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters</param>
        /// <param name="userId">User ID</param>
        [HttpGet("user/{userId}")]
        public Task<IActionResult> GetSavedCollectionsByUser(
            [FromQuery] PaginationParameter paginationParameter,
            long userId)
        {
            return ValidateAndExecute(async () => await _saveCollectionService.GetSavedCollectionsByUserAsync(paginationParameter, userId));
        }

        /// <summary>
        /// Check if a collection is saved by a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="collectionId">Collection ID</param>
        [HttpGet("check")]
        public Task<IActionResult> CheckIfCollectionSaved([FromQuery] long userId, [FromQuery] long collectionId)
        {
            return ValidateAndExecute(async () => await _saveCollectionService.CheckIfCollectionSavedAsync(userId, collectionId));
        }
    }
}
