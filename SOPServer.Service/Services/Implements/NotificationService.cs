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

        public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

            // Get all users to create user notifications
            var allUsers = await _unitOfWork.UserRepository.GetQueryable()
                .Where(u => !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync();

            var userNotifications = allUsers.Select(userId => new UserNotification
            {
                NotificationId = notification.Id,
                UserId = userId,
                IsRead = false
            }).ToList();

            await _unitOfWork.UserNotificationRepository.AddRangeAsync(userNotifications);
            await _unitOfWork.SaveAsync();

            // Get all user devices for push notifications
            var userDevices = await _unitOfWork.UserDeviceRepository.GetAllWithUser();
            
            if (userDevices.Any())
            {
                var tokens = userDevices.Select(x => x.DeviceToken).Distinct().ToList();

                var tokensNotValid = await FirebaseLibrary.SendRangeMessageFireBase(model.Title, model.Message, tokens);
                if (tokensNotValid.Any())
                {
                    await RemoveTokenNotValid(tokensNotValid);
                }
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.PUSH_NOTIFICATION_SUCCESS,
                Data = new
                {
                    NotificationsSent = allUsers.Count,
                    DevicesNotified = userDevices.Count
                }
            };
        }

        public async Task<BaseResponseModel> CreateNotification(NotificationRequestModel model)
        {
            var notification = _mapper.Map<Notification>(model);
            notification.Type = NotificationType.SYSTEM;

            await _unitOfWork.NotificationRepository.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.NOTIFICATION_CREATE_SUCCESS,
                Data = _mapper.Map<SystemNotificationModel>(notification)
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
    }
}
