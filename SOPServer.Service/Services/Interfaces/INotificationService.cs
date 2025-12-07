using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.NotificationModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface INotificationService
    {
        // Create
        Task<BaseResponseModel> PushNotificationByUserId(long userId, NotificationRequestModel model);
        Task<BaseResponseModel> PushSystemNotificationToUserAsync(long userId, NotificationRequestModel model);
        Task<BaseResponseModel> PushNotification(NotificationRequestModel model);
        // Read
        Task<BaseResponseModel> GetNotificationById(long id);
        Task<BaseResponseModel> GetNotificationsByUserId(PaginationParameter paginationParameter, long userId, int? type, bool? isRead);
        Task<BaseResponseModel> GetUnreadNotificationCount(long userId);
        Task<BaseResponseModel> GetSystemNotifications(PaginationParameter paginationParameter, bool newestFirst, string? searchTerm);
        Task<BaseResponseModel> GetAllNotifications(PaginationParameter paginationParameter);
        
        // Mark as Read (kept for UX purposes)
        Task<BaseResponseModel> MarkNotificationAsRead(long notificationId);
        Task<BaseResponseModel> MarkAllNotificationsAsRead(long userId);
        
        // Delete
        Task<BaseResponseModel> DeleteNotificationsByIdsAsync(long userId, DeleteNotificationsModel model);
        Task<BaseResponseModel> GetNotificationByUserNotificationId(long notiId);
    }
}
