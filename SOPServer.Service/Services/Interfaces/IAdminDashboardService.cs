using SOPServer.Service.BusinessModels.AdminDashboardModels;
using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<BaseResponseModel> GetRevenueStatisticsAsync(RevenueFilterModel filter);
        Task<BaseResponseModel> GetDashboardOverviewAsync();
        Task<BaseResponseModel> GetUserGrowthByMonthAsync(int? year = null);
        Task<BaseResponseModel> GetItemsByCategoryAsync();
        Task<BaseResponseModel> GetWeeklyActivityAsync();
    }
}
