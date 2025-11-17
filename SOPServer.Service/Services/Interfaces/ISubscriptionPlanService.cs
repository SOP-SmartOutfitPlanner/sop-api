using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SubscriptionPlanModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ISubscriptionPlanService
    {
        Task<BaseResponseModel> GetAllAsync();
        Task<BaseResponseModel> GetByIdAsync(long id);
        Task<BaseResponseModel> CreateAsync(SubscriptionPlanRequestModel model);
        Task<BaseResponseModel> UpdateAsync(long id, SubscriptionPlanRequestModel model);
        Task<BaseResponseModel> DeleteAsync(long id);
    }
}
