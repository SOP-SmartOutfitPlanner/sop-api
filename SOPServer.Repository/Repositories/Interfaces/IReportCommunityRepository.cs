using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.Repositories.Generic;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IReportCommunityRepository : IGenericRepository<ReportCommunity>
    {
        Task<ReportCommunity?> GetExistingReportAsync(long userId, long? postId, long? commentId, ReportType type);
        Task<(List<ReportCommunity>, int)> GetPendingReportsAsync(ReportType? type, DateTime? fromDate, DateTime? toDate, PaginationParameter pagination);
        Task<(List<ReportCommunity>, int)> GetAllReportsAsync(ReportType? type, ReportStatus? status, DateTime? fromDate, DateTime? toDate, PaginationParameter pagination);
        Task<ReportCommunity?> GetReportDetailsAsync(long reportId);
    }
}
