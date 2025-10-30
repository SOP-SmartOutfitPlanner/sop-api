using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.JobModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/jobs")]
    [ApiController]
    [Authorize]
    public class JobController : BaseController
    {
        private readonly IJobService _jobService;

        public JobController(IJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpGet]
        public Task<IActionResult> GetAll()
        {
            return ValidateAndExecute(async () => await _jobService.GetAllAsync());
        }

        [HttpGet("{id:long}")]
        public Task<IActionResult> GetById(long id)
        {
            return ValidateAndExecute(async () => await _jobService.GetByIdAsync(id));
        }

        [HttpPost]
        public Task<IActionResult> Create([FromBody] JobRequestModel model)
        {
            return ValidateAndExecute(async () => await _jobService.CreateAsync(model));
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> Update(long id, [FromBody] JobRequestModel model)
        {
            return ValidateAndExecute(async () => await _jobService.UpdateAsync(id, model));
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN")]
        public Task<IActionResult> Delete(long id)
        {
            return ValidateAndExecute(async () => await _jobService.DeleteAsync(id));
        }
    }
}
