using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class UserNotificationRepository : GenericRepository<UserNotification>, IUserNotificationRepository
    {
        private readonly SOPServerContext _context;

        public UserNotificationRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> GetUnreadNotificationCount(long userId)
        {
            return await _context.Set<UserNotification>()
                .Where(un => un.UserId == userId && !un.IsRead && !un.IsDeleted)
                .CountAsync();
        }

        public async Task<List<UserNotification>> GetUnreadNotificationByUserId(long userId)
        {
            return await _context.Set<UserNotification>()
                .Where(un => un.UserId == userId && !un.IsRead && !un.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<UserNotification>> GetUserNotificationsByIdsAsync(List<long> notificationIds, long userId)
        {
            return await _context.Set<UserNotification>()
                .Where(un => notificationIds.Contains(un.NotificationId)
                    && un.UserId == userId
                    && !un.IsDeleted)
                .ToListAsync();
        }
    }
}
