using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.AuthenModels;
using SOPServer.Service.BusinessModels.OnboardingModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IUserService
    {
        Task<BaseResponseModel> GetUserByIdAsync(long userId);
        Task<BaseResponseModel> GetUserProfileByIdAsync(long userId);
        Task<BaseResponseModel> LoginWithGoogleOAuth(string credential);
        Task<BaseResponseModel> RefreshToken(string jwtToken);
        Task<BaseResponseModel> UpdateUser(UpdateUserModel user);
        Task<BaseResponseModel> UpdateProfile(long userId, UpdateProfileModel model);
        Task<BaseResponseModel> UpdateAvatar(long userId, IFormFile file);
        Task<BaseResponseModel> GetUsers(PaginationParameter paginationParameter);
        Task<BaseResponseModel> SoftDeleteUserAsync(long userId);
        Task<BaseResponseModel> UpdateUserAddress(UpdateUserAddressModel userAddress);
        Task<BaseResponseModel> LoginWithEmailAndPassword(LoginRequestModel model);
        Task<BaseResponseModel> RegisterUser(RegisterRequestModel model);
        Task<BaseResponseModel> ResendOtp(string email);
        Task<BaseResponseModel> VerifyOtp(VerifyOtpRequestModel model);
        Task<BaseResponseModel> LogoutCurrentAsync(ClaimsPrincipal principal);
        Task<BaseResponseModel> SubmitOnboardingAsync(long userId, OnboardingRequestModel requestModel);
        Task<BaseResponseModel> InitiatePasswordResetAsync(string email);
        Task<BaseResponseModel> VerifyResetOtpAsync(VerifyResetOtpRequestModel model);
        Task<BaseResponseModel> ResetPasswordAsync(ResetPasswordRequestModel model);
        Task<UserCharacteristicModel> GetUserCharacteristic(long userId);
        Task<BaseResponseModel> GetStylistProfileByUserIdAsync(long userId, long? currentUserId = null);
        Task<BaseResponseModel> ChangePasswordAsync(long userId, ChangePasswordModel model);
        Task<BaseResponseModel> InitiateChangePasswordWithOtpAsync(long userId);
        Task<BaseResponseModel> ChangePasswordWithOtpAsync(long userId, ChangePasswordWithOtpModel model);
        Task<BaseResponseModel> ValidateFullBodyImageAsync(string imageUrl);
    }
}
