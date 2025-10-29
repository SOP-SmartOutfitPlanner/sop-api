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

            CreateMap<UserStyle, UserStyleModel>()
                .ForMember(dest => dest.StyleId, opt => opt.MapFrom(src => src.StyleId ?? 0))
                .ForMember(dest => dest.StyleName, opt => opt.MapFrom(src => src.Style != null ? src.Style.Name : string.Empty))
                .ForMember(dest => dest.StyleDescription, opt => opt.MapFrom(src => src.Style != null ? src.Style.Description : string.Empty));
        }
    }
}
