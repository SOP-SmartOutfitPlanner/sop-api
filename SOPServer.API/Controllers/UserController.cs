using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.OnboardingModels;
using SOPServer.Service.BusinessModels.UserModels;
using SOPServer.Service.Services.Interfaces;
using Microsoft.AspNetCore.Http;

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
        /// Update user profile information
        /// </summary>
        /// <remarks>
        /// **Auth Required**
        ///
        /// **Note:** UserId is extracted from JWT token automatically
        ///
        /// **Fields that can be updated:**
        /// - DisplayName
        /// - Dob (Date of Birth)
        /// - Gender
        /// - PreferedColor (List of preferred colors)
        /// - AvoidedColor (List of avoided colors)
        /// - Location
        /// - Bio
        /// - JobId
        /// - StyleIds (List of style IDs)
        /// </remarks>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);

            return await ValidateAndExecute(() => _userService.UpdateProfile(userId, model));
        }

        /// <summary>
        /// Update user avatar by uploading an image file
        /// </summary>
        /// <remarks>
        /// **Auth Required**
        ///
        /// **Note:** UserId is extracted from JWT token automatically
        ///
        /// **File Requirements:**
        /// - Accepted formats: .jpg, .jpeg, .png, .webp, .bmp, .gif, .tiff, .heic
        /// - Image will be uploaded to MinIO storage
        /// - Old avatar will be deleted automatically
        /// </remarks>
        [Authorize]
        [HttpPatch("profile/avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            long.TryParse(userIdClaim, out long userId);

            return await ValidateAndExecute(() => _userService.UpdateAvatar(userId, file));
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
