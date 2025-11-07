using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.FollowerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Mappers
{
    public class FollowerProfile : Profile
    {
        public FollowerProfile()
        {
            CreateMap<CreateFollowerModel, Follower>();
            CreateMap<Follower, FollowerModel>();
            
            // Map follower relationship to FollowerUserModel
            CreateMap<Follower, FollowerUserModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.FollowerId))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.FollowerUser.DisplayName))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.FollowerUser.AvtUrl))
                .ForMember(dest => dest.Bio, opt => opt.MapFrom(src => src.FollowerUser.Bio))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate));
            
            CreateMap<Pagination<Follower>, Pagination<FollowerUserModel>>()
                .ConvertUsing<PaginationConverter<Follower, FollowerUserModel>>();
        }
    }
}
