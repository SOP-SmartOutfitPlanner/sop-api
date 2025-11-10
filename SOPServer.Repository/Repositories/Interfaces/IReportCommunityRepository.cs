using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.Repositories.Generic;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IReportCommunityRepository : IGenericRepository<ReportCommunity>
    {
        Task<ReportCommunity?> GetExistingReportAsync(long userId, long? postId, long? commentId, ReportType type);
    }
}
