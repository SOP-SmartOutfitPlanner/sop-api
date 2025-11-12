using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.NotificationModels;

namespace SOPServer.Service.Mappers
{
    public class NotificationMapperProfile : Profile
    {
        public NotificationMapperProfile()
        {
            // UserNotification to NotificationModel (for user-specific notifications)
            CreateMap<UserNotification, NotificationModel>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Notification.Title))
                .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Notification.Message))
                .ForMember(dest => dest.Href, opt => opt.MapFrom(src => src.Notification.Href))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Notification.Type))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Notification.ImageUrl))
                .ForMember(dest => dest.ActorUserId, opt => opt.MapFrom(src => src.Notification.ActorUserId))
                .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Notification.ActorUser != null ? src.Notification.ActorUser.DisplayName : null))
                .ForMember(dest => dest.ActorAvatarUrl, opt => opt.MapFrom(src => src.Notification.ActorUser != null ? src.Notification.ActorUser.AvtUrl : null))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate));

            // Notification to NotificationModel (for direct notification retrieval)
            CreateMap<Notification, NotificationModel>()
                .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.ActorUser != null ? src.ActorUser.DisplayName : null))
                .ForMember(dest => dest.ActorAvatarUrl, opt => opt.MapFrom(src => src.ActorUser != null ? src.ActorUser.AvtUrl : null))
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false)) // Default to false for direct notification
                .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => (DateTime?)null))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate));

            // Notification to SystemNotificationModel
            CreateMap<Notification, SystemNotificationModel>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate));

            // NotificationRequestModel to Notification
            CreateMap<NotificationRequestModel, Notification>();

            // Pagination mappings
            CreateMap<Pagination<UserNotification>, Pagination<NotificationModel>>()
                .ConvertUsing<PaginationConverter<UserNotification, NotificationModel>>();

            CreateMap<Pagination<Notification>, Pagination<SystemNotificationModel>>()
                .ConvertUsing<PaginationConverter<Notification, SystemNotificationModel>>();
        }
    }
}
