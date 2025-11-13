using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CollectionModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/collections")]
    [ApiController]
    public class CollectionController : BaseController
    {
        private readonly ICollectionService _collectionService;

        public CollectionController(ICollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        [HttpGet]
        public Task<IActionResult> GetAllCollections([FromQuery] PaginationParameter paginationParameter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? callerUserId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                callerUserId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _collectionService.GetAllCollectionsPaginationAsync(paginationParameter, callerUserId));
        }

        [HttpGet("user/{userId}")]
        public Task<IActionResult> GetCollectionsByUser(long userId, [FromQuery] PaginationParameter paginationParameter)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? callerUserId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                callerUserId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _collectionService.GetCollectionsByUserPaginationAsync(paginationParameter, userId, callerUserId));
        }

        [HttpGet("{id}")]
        public Task<IActionResult> GetCollectionById(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long? callerUserId = null;
            if (long.TryParse(userIdClaim, out long parsedUserId))
            {
                callerUserId = parsedUserId;
            }

            return ValidateAndExecute(async () => await _collectionService.GetCollectionByIdAsync(id, callerUserId));
        }

        [HttpPost]
        public Task<IActionResult> CreateCollection([FromForm] CollectionCreateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _collectionService.CreateCollectionAsync(userId, model));
        }

        [HttpPut("{id}")]
        public Task<IActionResult> UpdateCollection(long id, [FromForm] CollectionUpdateModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _collectionService.UpdateCollectionAsync(id, userId, model));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteCollection(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _collectionService.DeleteCollectionAsync(id, userId));
        }

        [HttpPut("{id}/toggle-publish")]
        public Task<IActionResult> TogglePublishCollection(long id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);
            return ValidateAndExecute(async () => await _collectionService.TogglePublishCollectionAsync(id, userId));
        }
    }
}
