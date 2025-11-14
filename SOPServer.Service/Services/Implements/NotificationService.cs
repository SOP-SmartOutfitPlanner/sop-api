using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.NotificationModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SOPServer.Service.Utils;

namespace SOPServer.Service.Services.Implements
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IServiceScopeFactory _scopeFactory;

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper, IServiceScopeFactory scopeFactory)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _scopeFactory = scopeFactory;
        }

        public async Task<BaseResponseModel> GetNotificationById(long id)
        {
            var notification = await _unitOfWork.NotificationRepository.GetByIdIncludeAsync(
                id,
                include: query => query.Include(n => n.ActorUser)
            );

            if (notification == null)
            {
                throw new NotFoundException(MessageConstants.NOTIFICATION_NOT_EXIST);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Data = _mapper.Map<NotificationModel>(notification),
                Message = MessageConstants.GET_NOTIFICATION_SUCCESS
            };
        }

        public async Task<BaseResponseModel> PushNotificationByUserId(long userId, NotificationRequestModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var actor = await _unitOfWork.UserRepository.GetByIdAsync((long)model.ActorUserId);

            if (actor == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var notification = _mapper.Map<Notification>(model);
            notification.Type = NotificationType.USER;

            await _unitOfWork.NotificationRepository.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            var userNotification = new UserNotification
            {
                NotificationId = notification.Id,
                UserId = userId,
                IsRead = false
            };

            await _unitOfWork.UserNotificationRepository.AddAsync(userNotification);
            await _unitOfWork.SaveAsync();

            // Send push notification to user devices
            var userDevices = await _unitOfWork.UserDeviceRepository.GetUserDeviceByUserId(userId);

            if (!userDevices.Any())
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.PUSH_NOTIFICATION_USER_SUCCESS,
                    Data = new { Warning = MessageConstants.USER_DEVICE_NOT_FOUND }
                };
            }

            foreach (var device in userDevices)
            {
                await FirebaseLibrary.SendMessageFireBase(model.Title, model.Message, device.DeviceToken);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.PUSH_NOTIFICATION_USER_SUCCESS
            };
        }

        public async Task<BaseResponseModel> GetNotificationsByUserId(PaginationParameter paginationParameter, long userId, int type = 0)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (!Enum.TryParse(type.ToString(), out NotificationType notificationType))
            {
                throw new BadRequestException(MessageConstants.ENUM_NOTIFICATION_NOT_VALID);
            }

            var notifications = await _unitOfWork.UserNotificationRepository.ToPaginationIncludeAsync(
                paginationParameter,
                filter: x => x.UserId == userId && !x.IsDeleted && x.Notification.Type == notificationType,
                include: query => query
                    .Include(x => x.Notification)
                        .ThenInclude(n => n.ActorUser)
                    .Include(x => x.User),
                orderBy: query => query.OrderByDescending(x => x.CreatedDate)
            );

            var listNotifications = _mapper.Map<Pagination<NotificationModel>>(notifications);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_NOTIFICATION_SUCCESS,
                Data = new ModelPaging
                {
                    Data = listNotifications,
                    MetaData = new
                    {
                        listNotifications.TotalCount,
                        listNotifications.PageSize,
                        listNotifications.CurrentPage,
                        listNotifications.TotalPages,
                        listNotifications.HasNext,
                        listNotifications.HasPrevious
                    }
                }
            };
        }
        public async Task<BaseResponseModel> GetUnreadNotificationCount(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var count = await _unitOfWork.UserNotificationRepository.GetUnreadNotificationCount(userId);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.COUNT_UNREAD_NOTIFICATION_SUCCESS,
                Data = count
            };
        }

        public async Task<BaseResponseModel> MarkNotificationAsRead(long notificationId)
        {
            var userNotification = await _unitOfWork.UserNotificationRepository.GetByIdAsync(notificationId);

            if (userNotification == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOTIFICATION_NOT_EXIST);
            }

            userNotification.IsRead = true;
            userNotification.ReadAt = DateTime.UtcNow;

            _unitOfWork.UserNotificationRepository.UpdateAsync(userNotification);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.MARK_NOTIFICATION_AS_READ_SUCCESS
            };
        }

        public async Task<BaseResponseModel> MarkAllNotificationsAsRead(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var unreadNotifications = await _unitOfWork.UserNotificationRepository.GetUnreadNotificationByUserId(userId);

            if (!unreadNotifications.Any())
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.NO_NOTIFICATION_MARK_AS_READ
                };
            }

            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
                _unitOfWork.UserNotificationRepository.UpdateAsync(notification);
            }

            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.MARK_ALL_NOTIFICATION_AS_READ_SUCCESS
            };
        }

        public async Task<BaseResponseModel> DeleteNotificationsByIdsAsync(long userId, DeleteNotificationsModel model)
        {
            // Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Get UserNotification records for the specified notification IDs belonging to this user
            // This removes the relationship between the user and the notifications
            var userNotificationsToDelete = await _unitOfWork.UserNotificationRepository
                .GetUserNotificationsByIdsAsync(model.NotificationIds, userId);

            if (!userNotificationsToDelete.Any())
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = MessageConstants.NO_NOTIFICATIONS_TO_DELETE
                };
            }

            // Soft delete the UserNotification records (removes user's relationship with these notifications)
            foreach (var userNotification in userNotificationsToDelete)
            {
                _unitOfWork.UserNotificationRepository.SoftDeleteAsync(userNotification);
            }

            await _unitOfWork.SaveAsync();

            // Check if some notifications were not found
            var deletedCount = userNotificationsToDelete.Count;
            var requestedCount = model.NotificationIds.Count;

            var message = deletedCount == requestedCount
                ? MessageConstants.DELETE_NOTIFICATIONS_SUCCESS
                : MessageConstants.SOME_NOTIFICATIONS_NOT_FOUND;

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = message,
                Data = new
                {
                    DeletedCount = deletedCount,
                    RequestedCount = requestedCount
                }
            };
        }

        public async Task<BaseResponseModel> GetSystemNotifications(PaginationParameter paginationParameter, bool newestFirst, string? searchTerm)
        {
            var notifications = await _unitOfWork.NotificationRepository.ToPaginationIncludeAsync(
                paginationParameter,
                filter: x => !x.IsDeleted &&
                          x.Type == NotificationType.SYSTEM &&
                          (string.IsNullOrWhiteSpace(searchTerm) ||
                           EF.Functions.Collate(x.Title, "Latin1_General_CI_AI").Contains(EF.Functions.Collate(searchTerm, "Latin1_General_CI_AI")) ||
                           EF.Functions.Collate(x.Message, "Latin1_General_CI_AI").Contains(EF.Functions.Collate(searchTerm, "Latin1_General_CI_AI"))),
                orderBy: query => newestFirst
                    ? query.OrderByDescending(x => x.CreatedDate)
                    : query.OrderBy(x => x.CreatedDate)
            );

            var listNotifications = _mapper.Map<Pagination<SystemNotificationModel>>(notifications);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_LIST_NOTIFICATION_SUCCESS,
                Data = new ModelPaging
                {
                    Data = listNotifications,
                    MetaData = new
                    {
                        listNotifications.TotalCount,
                        listNotifications.PageSize,
                        listNotifications.CurrentPage,
                        listNotifications.TotalPages,
                        listNotifications.HasNext,
                        listNotifications.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> PushNotification(NotificationRequestModel model)
        {
            var notification = _mapper.Map<Notification>(model);
            notification.Type = NotificationType.SYSTEM;

            await _unitOfWork.NotificationRepository.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            // Fire-and-forget background task to create user notifications and send Firebase
            _ = ProcessNotificationInBackground(notification.Id, model.Title, model.Message);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.PUSH_NOTIFICATION_SUCCESS,
                Data = new
                {
                    NotificationId = notification.Id,
                    Message = MessageConstants.NOTIFICATION_PROCESS_IN_BACKGROUND
                }
            };
        }

        public async Task<BaseResponseModel> GetAllNotifications(PaginationParameter paginationParameter)
        {
            var notifications = await _unitOfWork.NotificationRepository.ToPaginationIncludeAsync(
                paginationParameter,
                filter: x => !x.IsDeleted,
                include: query => query.Include(n => n.ActorUser),
                orderBy: query => query.OrderByDescending(x => x.CreatedDate)
            );

            var listNotifications = _mapper.Map<Pagination<SystemNotificationModel>>(notifications);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_ALL_NOTIFICATIONS_SUCCESS,
                Data = new ModelPaging
                {
                    Data = listNotifications,
                    MetaData = new
                    {
                        listNotifications.TotalCount,
                        listNotifications.PageSize,
                        listNotifications.CurrentPage,
                        listNotifications.TotalPages,
                        listNotifications.HasNext,
                        listNotifications.HasPrevious
                    }
                }
            };
        }

        private async Task RemoveTokenNotValid(List<string> tokens)
        {
            foreach (var token in tokens)
            {
                var userDevice = await _unitOfWork.UserDeviceRepository.GetByTokenDevice(token);
                if (userDevice != null)
                {
                    _unitOfWork.UserDeviceRepository.SoftDeleteAsync(userDevice);
                }
            }
            await _unitOfWork.SaveAsync();
        }

        private async Task ProcessNotificationInBackground(long notificationId, string title, string message)
        {
            // Create a new scope so we use scoped services safely in a background task
            using var scope = _scopeFactory.CreateScope();
            var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                // Get all users to create user notifications (in background, không block API response)
                var allUsers = await scopedUnitOfWork.UserRepository.GetQueryable()
                        .Where(u => !u.IsDeleted)
                        .Select(u => u.Id)
                        .ToListAsync();

                // Batch create user notifications
                var userNotifications = allUsers.Select(userId => new UserNotification
                {
                    NotificationId = notificationId,
                    UserId = userId,
                    IsRead = false
                }).ToList();

                await scopedUnitOfWork.UserNotificationRepository.AddRangeAsync(userNotifications);
                await scopedUnitOfWork.SaveAsync();

                // Get all user devices for push notifications
                var userDevices = await scopedUnitOfWork.UserDeviceRepository.GetAllWithUser();

                if (userDevices.Any())
                {
                    var tokens = userDevices.Select(x => x.DeviceToken).Distinct().ToList();

                    var tokensNotValid = await FirebaseLibrary.SendRangeMessageFireBase(title, message, tokens);

                    if (tokensNotValid.Any())
                    {
                        // Remove invalid tokens
                        foreach (var token in tokensNotValid)
                        {
                            var userDevice = await scopedUnitOfWork.UserDeviceRepository.GetByTokenDevice(token);
                            if (userDevice != null)
                            {
                                scopedUnitOfWork.UserDeviceRepository.SoftDeleteAsync(userDevice);
                            }
                        }
                        await scopedUnitOfWork.SaveAsync();
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public async Task<BaseResponseModel> GetNotificationByUserNotificationId(long notiId)
        {
            var userNotification = await _unitOfWork.UserNotificationRepository.GetByIdIncludeAsync(
                notiId,
                include: query => query
                    .Include(un => un.Notification)
                        .ThenInclude(n => n.ActorUser)
                    .Include(un => un.User)
            );

            if (userNotification == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOTIFICATION_NOT_EXIST);
            }

            var notification = _mapper.Map<NotificationModel>(userNotification.Notification);
            notification.Id = userNotification.Id;
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Data = notification,
                Message = MessageConstants.GET_NOTIFICATION_SUCCESS
            };
        }
    }
}
