using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.ReportCommunityModels;
using SOPServer.Service.BusinessModels.UserModels;

namespace SOPServer.Service.Mappers
{
    public class ReportCommunityMapperProfile : Profile
    {
        public ReportCommunityMapperProfile()
        {
            CreateMap<ReportCommunityCreateModel, ReportCommunity>();
            CreateMap<ReportCommunity, ReportCommunityModel>();

            CreateMap<User, UserBasicModel>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvtUrl));

            CreateMap<Post, ReportedContentModel>()
                .ForMember(dest => dest.ContentId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => "POST"));

            CreateMap<CommentPost, ReportedContentModel>()
                .ForMember(dest => dest.ContentId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => "COMMENT"))
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Comment));
        }
    }
}
