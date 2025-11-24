using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class ReportReporterRepository : GenericRepository<ReportReporter>, IReportReporterRepository
    {
        private readonly SOPServerContext _context;

        public ReportReporterRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ReportReporter?> GetByReportAndUserAsync(long reportId, long userId)
        {
            return await _context.ReportReporters
                .Where(rr => !rr.IsDeleted && rr.ReportId == reportId && rr.UserId == userId)
                .FirstOrDefaultAsync();
        }
    }
}
