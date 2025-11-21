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
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => "POST"))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => 
                    src.PostImages != null 
                        ? src.PostImages.Where(pi => !pi.IsDeleted).Select(pi => pi.ImgUrl).ToList() 
                        : new List<string>()));

            CreateMap<CommentPost, ReportedContentModel>()
                .ForMember(dest => dest.ContentId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => "COMMENT"))
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Comment))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => new List<string>())); // Comments don't have images
        }
    }
}
