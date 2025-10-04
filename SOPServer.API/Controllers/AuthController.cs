using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SOPServer.Service.BusinessModels.AuthenModels;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IOtpService _otpService;

        public AuthController(IUserService userService, IOtpService otpService)
        {
            _userService = userService;
            _otpService = otpService;
        }

        /// <summary>
        /// Login with Google. If user not existed, send OTP through gmail.
        /// </summary>
        [HttpPost("login/google/oauth")]
        public Task<IActionResult> LoginWithGoogleOAuth([FromBody] string credential)
        {
            return ValidateAndExecute(() => _userService.LoginWithGoogleOAuth(credential));
        }

        [HttpPost("refresh-token")]
        public Task<IActionResult> RefreshToken([FromBody] string token)
        {
            return ValidateAndExecute(() => _userService.RefreshToken(token));
        }

        /// <summary>
        /// Register and send OTP through gmail 
        /// </summary>
        [HttpPost("register")]
        public Task<IActionResult> Register([FromBody] RegisterRequestModel model)
        {
            return ValidateAndExecute(() => _userService.RegisterUser(model));
        }

        /// <summary>
        /// Verify otp
        /// </summary>
        [HttpPost("otp/verify")]
        public Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestModel model)
        {
            return ValidateAndExecute(() => _userService.VerifyOtp(model));
        }

        /// <summary>
        /// Resend otp
        /// </summary>
        [HttpPost("otp/resend")]
        public Task<IActionResult> ResendOtp([FromBody] SendOtpRequestModel model)
        {
            return ValidateAndExecute(() => _userService.ResendOtp(model.Email));
        }

        [HttpPost]
        public Task<IActionResult> LoginWithEmailAndPassword([FromBody] LoginRequestModel model)
        {
            return ValidateAndExecute(() => _userService.LoginWithEmailAndPassword(model));
        }
    }
}

