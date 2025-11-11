using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using PayOS.Models.V2.PaymentRequests;
using SOPServer.Service.BusinessModels.AuthenModels;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/auth")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IOtpService _otpService;
        private readonly IPayOSService _payOSService;
        public AuthController(IUserService userService, IOtpService otpService, IPayOSService payOSService)
        {
            _userService = userService;
            _otpService = otpService;
            _payOSService = payOSService;
        }
        [HttpPost("payos/{id:int}")]
        public Task<CreatePaymentLinkResponse> CreatePaymentLink(int id)
        {
            return _payOSService.CreatePaymentUrl(id);
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

        [Authorize]
        [HttpPost("logout")]
        public Task<IActionResult> LogoutCurrent()
        {
            return ValidateAndExecute(() => _userService.LogoutCurrentAsync(User));
        }

        [HttpPost]
        public Task<IActionResult> LoginWithEmailAndPassword([FromBody] LoginRequestModel model)
        {
            return ValidateAndExecute(() => _userService.LoginWithEmailAndPassword(model));
        }

        /// <summary>
        /// Initiate password reset - sends OTP to email if account exists and uses password login
        /// </summary>
        [HttpPost("password/forgot")]
        public Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestModel model)
        {
            return ValidateAndExecute(() => _userService.InitiatePasswordResetAsync(model.Email));
        }

        /// <summary>
        /// Verify OTP for password reset - returns reset token
        /// </summary>
        [HttpPost("password/verify-otp")]
        public Task<IActionResult> VerifyResetOtp([FromBody] VerifyResetOtpRequestModel model)
        {
            return ValidateAndExecute(() => _userService.VerifyResetOtpAsync(model));
        }

        /// <summary>
        /// Reset password using reset token
        /// </summary>
        [HttpPost("password/reset")]
        public Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestModel model)
        {
            return ValidateAndExecute(() => _userService.ResetPasswordAsync(model));
        }
    }
}

