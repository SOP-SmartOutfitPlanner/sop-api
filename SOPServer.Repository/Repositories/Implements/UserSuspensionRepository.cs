using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class UserSuspensionRepository : GenericRepository<UserSuspension>, IUserSuspensionRepository
    {
        private readonly SOPServerContext _context;

        public UserSuspensionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<UserSuspension?> GetActiveSuspensionAsync(long userId)
        {
            return await _context.UserSuspensions
                .Where(s => !s.IsDeleted
                    && s.UserId == userId
                    && s.IsActive
                    && s.EndAt > DateTime.UtcNow)
                .OrderByDescending(s => s.EndAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserSuspension>> GetSuspensionHistoryAsync(long userId)
        {
            return await _context.UserSuspensions
                .Where(s => !s.IsDeleted && s.UserId == userId)
                .OrderByDescending(s => s.StartAt)
                .ToListAsync();
        }
    }
}
