using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IReportReporterRepository : IGenericRepository<ReportReporter>
    {
        Task<ReportReporter?> GetByReportAndUserAsync(long reportId, long userId);
    }
}
