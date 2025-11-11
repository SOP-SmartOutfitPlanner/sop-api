using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.LikeCollectionModels;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/like-collections")]
    [ApiController]
    public class LikeCollectionController : BaseController
    {
        private readonly ILikeCollectionService _likeCollectionService;

        public LikeCollectionController(ILikeCollectionService likeCollectionService)
        {
            _likeCollectionService = likeCollectionService;
        }

        /// <summary>
        /// Create a new like on a collection (or toggle existing like)
        /// </summary>
        /// <param name="model">Like collection creation model</param>
        /// <returns>Created like collection</returns>
        [HttpPost]
        public Task<IActionResult> CreateLikeCollection([FromBody] CreateLikeCollectionModel model)
        {
            return ValidateAndExecute(async () => await _likeCollectionService.CreateLikeCollection(model));
        }
    }
}
