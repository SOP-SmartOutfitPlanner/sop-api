using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserOccasionModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IUserOccasionService
    {
        Task<BaseResponseModel> GetUserOccasionPaginationAsync(PaginationParameter paginationParameter, long userId);
        Task<BaseResponseModel> GetUserOccasionByIdAsync(long id, long userId);
        Task<BaseResponseModel> CreateUserOccasionAsync(long userId, UserOccasionCreateModel model);
        Task<BaseResponseModel> UpdateUserOccasionAsync(long id, long userId, UserOccasionUpdateModel model);
        Task<BaseResponseModel> DeleteUserOccasionAsync(long id, long userId);
    }
}
