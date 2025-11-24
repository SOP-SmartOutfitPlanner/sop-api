using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.Commons;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class ReportCommunityRepository : GenericRepository<ReportCommunity>, IReportCommunityRepository
    {
        private readonly SOPServerContext _context;
        public ReportCommunityRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ReportCommunity?> GetExistingReportByContentAsync(long? postId, long? commentId, ReportType type)
        {
            return await _context.ReportCommunities
                .Where(r => !r.IsDeleted
                    && r.Type == type
                    && r.PostId == postId
                    && r.CommentId == commentId
                    && r.Status == ReportStatus.PENDING)
                .FirstOrDefaultAsync();
        }

        public async Task<(List<ReportCommunity>, int)> GetPendingReportsAsync(
            ReportType? type,
            DateTime? fromDate,
            DateTime? toDate,
            PaginationParameter pagination)
        {
            var query = _context.ReportCommunities
                .Where(r => !r.IsDeleted && r.Status == ReportStatus.PENDING);

            // Apply filters
            if (type.HasValue)
            {
                query = query.Where(r => r.Type == type.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.CreatedDate <= toDate.Value);
            }

            var totalCount = await query.CountAsync();

            var reports = await query
                .Include(r => r.ReportReporters.Where(rr => !rr.IsDeleted).OrderBy(rr => rr.CreatedDate))
                    .ThenInclude(rr => rr.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.CommentPost)
                    .ThenInclude(c => c.User)
                .OrderByDescending(r => r.CreatedDate)
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return (reports, totalCount);
        }

        public async Task<(List<ReportCommunity>, int)> GetAllReportsAsync(
            ReportType? type,
            ReportStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            PaginationParameter pagination)
        {
            var query = _context.ReportCommunities
                .Where(r => !r.IsDeleted);

            // Apply filters
            if (type.HasValue)
            {
                query = query.Where(r => r.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.CreatedDate <= toDate.Value);
            }

            var totalCount = await query.CountAsync();

            var reports = await query
                .Include(r => r.ReportReporters.Where(rr => !rr.IsDeleted).OrderBy(rr => rr.CreatedDate))
                    .ThenInclude(rr => rr.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.CommentPost)
                    .ThenInclude(c => c.User)
                .Include(r => r.ResolvedByAdmin)
                .OrderByDescending(r => r.CreatedDate)
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return (reports, totalCount);
        }

        public async Task<ReportCommunity?> GetReportDetailsAsync(long reportId)
        {
            return await _context.ReportCommunities
                .Include(r => r.ReportReporters.Where(rr => !rr.IsDeleted).OrderBy(rr => rr.CreatedDate))
                    .ThenInclude(rr => rr.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p.PostImages)
                .Include(r => r.CommentPost)
                    .ThenInclude(c => c.User)
                .Include(r => r.CommentPost)
                    .ThenInclude(c => c.Post)
                .Include(r => r.ResolvedByAdmin)
                .FirstOrDefaultAsync(r => r.Id == reportId && !r.IsDeleted);
        }
    }
}
