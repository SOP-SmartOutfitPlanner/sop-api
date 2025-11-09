using Microsoft.AspNetCore.Http;
using SOPServer.Service.BusinessModels.EmailModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using System.Security.Cryptography;

namespace SOPServer.Service.Services.Implements
{
    public class OtpService : IOtpService
    {
        private readonly IRedisService _redisService;
        private readonly IMailService _mailService;
        private readonly IEmailTemplateService _emailTemplateService;
        private const int OTP_EXPIRY_MINUTES = 5;
        private const int MAX_ATTEMPTS_PER_15_MINUTES = 5;
        private const int ATTEMPT_WINDOW_MINUTES = 15;

        public OtpService(IRedisService redisService, IMailService mailService, IEmailTemplateService emailTemplateService)
        {
            _redisService = redisService;
            _mailService = mailService;
            _emailTemplateService = emailTemplateService;
        }

        public async Task<BaseResponseModel> SendOtpAsync(string email, string name)
        {
            var attemptKey = RedisKeyConstants.GetOtpAttemptKey(email);
            var attempts = await _redisService.IncrementAsync(
                attemptKey,
                TimeSpan.FromMinutes(ATTEMPT_WINDOW_MINUTES)
            );

            if (attempts > MAX_ATTEMPTS_PER_15_MINUTES)
            {
                throw new BadRequestException(MessageConstants.OTP_TOO_MANY_ATTEMPTS);
            }

            var otp = GenerateOtp();
            var otpKey = RedisKeyConstants.GetOtpKey(email);

            await _redisService.SetAsync(
                otpKey,
                otp,
                TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES)
            );

            try
            {
                var emailBody = await _emailTemplateService.GenerateOtpEmailAsync(new OtpEmailTemplateModel
                {
                    DisplayName = name,
                    Otp = otp,
                    ExpiryMinutes = OTP_EXPIRY_MINUTES
                });

                await _mailService.SendEmailAsync(new MailRequest
                {
                    ToEmail = email,
                    Subject = "Smart Outfit Planner - Verification Code",
                    Body = emailBody
                });

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.OTP_SENT_SUCCESS,
                    Data = new
                    {
                        ExpiryMinutes = OTP_EXPIRY_MINUTES,
                        RemainingAttempts = MAX_ATTEMPTS_PER_15_MINUTES - attempts
                    }
                };
            }
            catch (Exception)
            {
                await _redisService.RemoveAsync(otpKey);
                throw new Exception(MessageConstants.EMAIL_SEND_FAILED);
            }
        }

        public async Task<BaseResponseModel> VerifyOtpAsync(string email, string otp)
        {
            var otpKey = RedisKeyConstants.GetOtpKey(email);
            var storedOtp = await _redisService.GetAsync<string>(otpKey);

            if (string.IsNullOrEmpty(storedOtp))
            {
                throw new BadRequestException(MessageConstants.OTP_INVALID);
            }

            if (storedOtp != otp)
            {
                throw new BadRequestException(MessageConstants.OTP_INVALID);
            }

            var verifiedKey = RedisKeyConstants.GetVerifiedEmailKey(email);

            await _redisService.RemoveAsync(otpKey);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.OTP_VERIFY_SUCCESS
            };
        }

        public async Task<bool> IsEmailVerifiedAsync(string email)
        {
            var verifiedKey = RedisKeyConstants.GetVerifiedEmailKey(email);
            return await _redisService.ExistsAsync(verifiedKey);
        }

        private string GenerateOtp()
        {
            var randomNumber = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            var number = BitConverter.ToUInt32(randomNumber, 0);
            return (number % 1000000).ToString("D6");
        }
    }
}
