using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.AISettingModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    namespace SOPServer.API.Controllers
    {
        [Route("api/v1/ai-settings")]
        [ApiController]
        [Authorize(Roles = "ADMIN")]
        public class AISettingController : BaseController
        {
            private readonly IAISettingService _aiSettingService;

            public AISettingController(IAISettingService aiSettingService)
            {
                _aiSettingService = aiSettingService;
            }

            [HttpGet]
            public Task<IActionResult> GetAll()
            {
                return ValidateAndExecute(async () => await _aiSettingService.GetAllAsync());
            }

            [HttpGet("{id:long}")]
            public Task<IActionResult> GetById(long id)
            {
                return ValidateAndExecute(async () => await _aiSettingService.GetByIdAsync(id));
            }

            [HttpGet("type/{type}")]
            public Task<IActionResult> GetByType(AISettingType type)
            {
                return ValidateAndExecute(async () => await _aiSettingService.GetByTypeAsync(type));
            }

            [HttpPost]
            public Task<IActionResult> Create([FromBody] AISettingRequestModel model)
            {
                return ValidateAndExecute(async () => await _aiSettingService.CreateAsync(model));
            }

            [HttpPut("{id:long}")]
            public Task<IActionResult> Update(long id, [FromBody] AISettingRequestModel model)
            {
                return ValidateAndExecute(async () => await _aiSettingService.UpdateAsync(id, model));
            }

            [HttpDelete("{id:long}")]
            public Task<IActionResult> Delete(long id)
            {
                return ValidateAndExecute(async () => await _aiSettingService.DeleteAsync(id));
            }
        }
    }
}
