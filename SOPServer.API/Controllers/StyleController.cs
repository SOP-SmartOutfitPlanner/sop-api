using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.StyleModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/styles")]
    [ApiController]
    public class StyleController : BaseController
    {
        private readonly IStyleService _styleService;

        public StyleController(IStyleService styleService)
        {
            _styleService = styleService;
        }

        [HttpGet]
        public Task<IActionResult> GetStyles([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _styleService.GetStylePaginationAsync(paginationParameter));
        }

        [HttpGet("{id}")]
        public Task<IActionResult> GetStyleById(long id)
        {
            return ValidateAndExecute(async () => await _styleService.GetStyleByIdAsync(id));
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> CreateStyle([FromBody] StyleCreateModel model)
        {
            return ValidateAndExecute(async () => await _styleService.CreateStyleAsync(model));
        }

        [HttpPut]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> UpdateStyle([FromBody] StyleUpdateModel model)
        {
            return ValidateAndExecute(async () => await _styleService.UpdateStyleByIdAsync(model));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> DeleteStyle(long id)
        {
            return ValidateAndExecute(async () => await _styleService.DeleteStyleByIdAsync(id));
        }
    }
}
