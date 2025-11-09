using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.OnboardingModels;
using SOPServer.Service.BusinessModels.UserModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class UserMapperProfile : Profile
    {
        public UserMapperProfile()
        {
            CreateMap<OnboardingRequestModel, User>();

            // User List mapping
            CreateMap<User, UserListModel>()
                .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Job != null ? src.Job.Name : null))
                .ForMember(dest => dest.UserStyles, opt => opt.MapFrom(src => src.UserStyles != null ? src.UserStyles.ToList() : new List<UserStyle>()));

            CreateMap<Pagination<User>, Pagination<UserListModel>>()
                .ConvertUsing<PaginationConverter<User, UserListModel>>();

            // User Profile mapping
            CreateMap<User, UserProfileModel>()
                .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Job != null ? src.Job.Name : null))
                .ForMember(dest => dest.JobDescription, opt => opt.MapFrom(src => src.Job != null ? src.Job.Description : null))
                .ForMember(dest => dest.UserStyles, opt => opt.MapFrom(src => src.UserStyles != null ? src.UserStyles.ToList() : new List<UserStyle>()));

            // User Public mapping (no sensitive fields)
            CreateMap<User, UserPublicModel>()
                .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Job != null ? src.Job.Name : null))
                .ForMember(dest => dest.JobDescription, opt => opt.MapFrom(src => src.Job != null ? src.Job.Description : null))
                .ForMember(dest => dest.UserStyles, opt => opt.MapFrom(src => src.UserStyles != null ? src.UserStyles.ToList() : new List<UserStyle>()));

            // UserCharacteristic mapping
            CreateMap<User, UserCharacteristicModel>()
                .ForMember(dest => dest.Job, opt => opt.MapFrom(src => src.Job != null ? src.Job.Name : null))
                .ForMember(dest => dest.Styles, opt => opt.MapFrom(src => src.UserStyles != null
                    ? src.UserStyles
                        .Where(us => us.Style != null)
                        .Select(us => us.Style.Name)
                        .ToList()
                    : new List<string>()));

            CreateMap<UserStyle, UserStyleModel>()
                .ForMember(dest => dest.StyleId, opt => opt.MapFrom(src => src.StyleId ?? 0))
                .ForMember(dest => dest.StyleName, opt => opt.MapFrom(src => src.Style != null ? src.Style.Name : string.Empty))
                .ForMember(dest => dest.StyleDescription, opt => opt.MapFrom(src => src.Style != null ? src.Style.Description : string.Empty));
        }
    }
}
