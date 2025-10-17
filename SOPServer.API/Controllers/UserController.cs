using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.OnboardingModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/user")]
    [ApiController]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService) 
        {
            _userService = userService;
        }

        [Authorize]
        [HttpPost("onboarding")]
        public async Task<IActionResult> Submit([FromBody] OnboardingRequestModel requestModel)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);

            return await ValidateAndExecute(() => _userService.SubmitOnboardingAsync(userId, requestModel));
        }
    }
}
