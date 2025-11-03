using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.OnboardingModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.Utils;
using System.Security.Claims;

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

        //[Authorize(Roles = "ADMIN")]
        //todo add authorize
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] PaginationParameter paginationParameter)
        {
            return await ValidateAndExecute(() => _userService.GetUsers(paginationParameter));
        }

        /// <summary>
        /// Get user by ID (public information only)
        /// </summary>
        /// <remarks>
        /// **No Auth Required**
        ///
        /// Returns public user information without sensitive fields
        /// </remarks>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(long userId)
        {
            return await ValidateAndExecute(() => _userService.GetUserByIdAsync(userId));
        }

        /// <summary>
        /// Get user profile information
        /// </summary>
        /// <remarks>
        /// **Auth Required**
        ///
        /// **Note:** UserId is extracted from JWT token automatically
        /// </remarks>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);

            return await ValidateAndExecute(() => _userService.GetUserProfileByIdAsync(userId));
        }

        /// <summary>
        /// Submit onboarding information for first-time user setup
        /// </summary>
        /// <remarks>
        /// **Auth Required** 
        ///
        /// **Note:** UserId is extracted from JWT token automatically
        /// </remarks>
        [Authorize]
        [HttpPost("onboarding")]
        public async Task<IActionResult> Submit([FromBody] OnboardingRequestModel requestModel)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);

            return await ValidateAndExecute(() => _userService.SubmitOnboardingAsync(userId, requestModel));
        }

        //[Authorize(Roles = "ADMIN")]
        //todo add authorize...
        //ᓚᘏᗢ
        [HttpDelete("{userId}")]
        public async Task<IActionResult> SoftDeleteUser(long userId)
        {
            return await ValidateAndExecute(() => _userService.SoftDeleteUserAsync(userId));
        }
    }
}
