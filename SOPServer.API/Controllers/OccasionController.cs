using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/occasions")]
    [ApiController]
    public class OccasionController : BaseController
    {
        private readonly IOccasionService _occasionService;

        public OccasionController(IOccasionService occasionService)
        {
            _occasionService = occasionService;
        }

        /// <summary>
        /// Get all occasions with pagination
        /// </summary>
        [HttpGet]
        public Task<IActionResult> GetOccasions([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(() => _occasionService.GetOccasionPaginationAsync(paginationParameter));
        }

        /// <summary>
        /// Get occasion by ID
        /// </summary>
        [HttpGet("{id}")]
        public Task<IActionResult> GetOccasionById(long id)
        {
            return ValidateAndExecute(() => _occasionService.GetOccasionByIdAsync(id));
        }

        /// <summary>
        /// Create new occasion (Admin only)
        /// </summary>
        [HttpPost]
        public Task<IActionResult> CreateOccasion([FromBody] OccasionCreateModel model)
        {
            return ValidateAndExecute(() => _occasionService.CreateOccasionAsync(model));
        }

        /// <summary>
        /// Update occasion (Admin only)
        /// </summary>
        [HttpPut]
        public Task<IActionResult> UpdateOccasion([FromBody] OccasionUpdateModel model)
        {
            return ValidateAndExecute(() => _occasionService.UpdateOccasionByIdAsync(model));
        }

        /// <summary>
        /// Delete occasion (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteOccasion(long id)
        {
            return ValidateAndExecute(() => _occasionService.DeleteOccasionByIdAsync(id));
        }
    }
}
