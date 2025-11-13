using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserDeviceModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IUserDeviceService
    {
        Task<BaseResponseModel> AddDeviceTokenByUserId(CreateUserDeviceModel model);
        Task<BaseResponseModel> DeleteDeviceToken(string token);
    }
}
