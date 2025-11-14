using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class UserViolationRepository : GenericRepository<UserViolation>, IUserViolationRepository
    {
        private readonly SOPServerContext _context;

        public UserViolationRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> CountWarningsInPeriodAsync(long userId, DateTime since)
        {
            return await _context.UserViolations
                .Where(v => !v.IsDeleted
                    && v.UserId == userId
                    && v.ViolationType == "WARN"
                    && v.OccurredAt >= since)
                .CountAsync();
        }

        public async Task<List<UserViolation>> GetViolationHistoryAsync(long userId)
        {
            return await _context.UserViolations
                .Where(v => !v.IsDeleted && v.UserId == userId)
                .OrderByDescending(v => v.OccurredAt)
                .ToListAsync();
        }
    }
}
