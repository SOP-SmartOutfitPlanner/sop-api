using SOPServer.Service.BusinessModels.AdminDashboardModels;
using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<BaseResponseModel> GetRevenueStatisticsAsync(RevenueFilterModel filter);
    }
}
