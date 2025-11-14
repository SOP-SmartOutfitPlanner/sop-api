using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IUserNotificationRepository : IGenericRepository<UserNotification>
    {
        Task<int> GetUnreadNotificationCount(long userId);
        Task<List<UserNotification>> GetUnreadNotificationByUserId(long userId);
        Task<List<UserNotification>> GetUserNotificationsByIdsAsync(List<long> notificationIds, long userId);
    }
}
