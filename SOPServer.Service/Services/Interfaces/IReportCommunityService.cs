using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.ReportCommunityModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IReportCommunityService
    {
        Task<BaseResponseModel> CreateReportAsync(ReportCommunityCreateModel model);
    }
}
