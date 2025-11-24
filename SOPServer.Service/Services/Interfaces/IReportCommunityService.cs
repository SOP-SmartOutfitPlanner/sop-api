using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.ReportCommunityModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IReportCommunityService
    {
        Task<BaseResponseModel> CreateReportAsync(ReportCommunityCreateModel model);
        Task<BaseResponseModel> GetPendingReportsAsync(ReportFilterModel filter, PaginationParameter pagination);
        Task<BaseResponseModel> GetAllReportsAsync(ReportFilterModel filter, PaginationParameter pagination);
        Task<BaseResponseModel> GetReportDetailsAsync(long reportId);
        Task<BaseResponseModel> GetReportersByReportIdAsync(long reportId, PaginationParameter pagination);
        Task<BaseResponseModel> ResolveNoViolationAsync(long reportId, long adminId, ResolveNoViolationModel model);
        Task<BaseResponseModel> ResolveWithActionAsync(long reportId, long adminId, ResolveWithActionModel model);
    }
}
