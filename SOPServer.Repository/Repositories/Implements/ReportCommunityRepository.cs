using Microsoft.EntityFrameworkCore;
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

        public async Task<ReportCommunity?> GetExistingReportAsync(long userId, long? postId, long? commentId, ReportType type)
        {
            return await _context.ReportCommunities
                .Where(r => !r.IsDeleted 
                    && r.UserId == userId 
                    && r.Type == type
                    && r.PostId == postId
                    && r.CommentId == commentId)
                .FirstOrDefaultAsync();
        }
    }
}
