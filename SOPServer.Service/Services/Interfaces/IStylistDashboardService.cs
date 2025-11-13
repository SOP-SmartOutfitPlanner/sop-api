using SOPServer.Service.BusinessModels.DashboardModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IStylistDashboardService
    {
        Task<BaseResponseModel> GetCollectionStatisticsByUserAsync(long userId, DashboardFilterModel filter);
    }
}
