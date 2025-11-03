using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserOccasionModels;
using System;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IUserOccasionService
    {
        Task<BaseResponseModel> GetUserOccasionPaginationAsync(
            PaginationParameter paginationParameter,
            long userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? year = null,
            int? month = null,
            int? upcomingDays = null,
            bool? today = null);
        Task<BaseResponseModel> GetUserOccasionByIdAsync(long id, long userId);
        Task<BaseResponseModel> CreateUserOccasionAsync(long userId, UserOccasionCreateModel model);
        Task<BaseResponseModel> UpdateUserOccasionAsync(long id, long userId, UserOccasionUpdateModel model);
        Task<BaseResponseModel> DeleteUserOccasionAsync(long id, long userId);
    }
}
