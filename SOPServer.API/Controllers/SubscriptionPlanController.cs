using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.SubscriptionPlanModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/subscription-plans")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class SubscriptionPlanController : BaseController
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public SubscriptionPlanController(ISubscriptionPlanService subscriptionPlanService)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }

        [HttpGet]
        public Task<IActionResult> GetAll()
        {
            return ValidateAndExecute(async () => await _subscriptionPlanService.GetAllAsync());
        }

        [HttpGet("{id:long}")]
        public Task<IActionResult> GetById(long id)
        {
            return ValidateAndExecute(async () => await _subscriptionPlanService.GetByIdAsync(id));
        }

        [HttpPost]
        public Task<IActionResult> Create([FromBody] SubscriptionPlanRequestModel model)
        {
            return ValidateAndExecute(async () => await _subscriptionPlanService.CreateAsync(model));
        }

        [HttpPut("{id:long}")]
        public Task<IActionResult> Update(long id, [FromBody] SubscriptionPlanRequestModel model)
        {
            return ValidateAndExecute(async () => await _subscriptionPlanService.UpdateAsync(id, model));
        }

        [HttpDelete("{id:long}")]
        public Task<IActionResult> Delete(long id)
        {
            return ValidateAndExecute(async () => await _subscriptionPlanService.DeleteAsync(id));
        }
    }
}
