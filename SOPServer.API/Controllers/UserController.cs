using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
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

        //todo add authorize
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] PaginationParameter paginationParameter)
        {
            return await ValidateAndExecute(() => _userService.GetUsers(paginationParameter));
        }

        //todo add authorize too !!!
        [HttpGet("profile/{userId}")]
        public async Task<IActionResult> GetUserProfile(long userId)
        {
            return await ValidateAndExecute(() => _userService.GetUserProfileByIdAsync(userId));
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
