using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.AuthenModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IUserService
    {
        Task<BaseResponseModel> GetUserById(int id);
        Task<BaseResponseModel> LoginWithGoogleOAuth(string credential);
        Task<BaseResponseModel> RefreshToken(string jwtToken);
        Task<BaseResponseModel> UpdateUser(UpdateUserModel user);
        Task<BaseResponseModel> GetUsers(PaginationParameter paginationParameter);
        Task<BaseResponseModel> DeleteUser(int id);
        Task<BaseResponseModel> UpdateUserAddress(UpdateUserAddressModel userAddress);
        Task<BaseResponseModel> LoginWithEmailAndPassword(LoginRequestModel model);
        Task<BaseResponseModel> RegisterUser(RegisterRequestModel model);
    }
}
