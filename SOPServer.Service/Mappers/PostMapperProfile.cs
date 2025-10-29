using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.PostModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class PostMapperProfile : Profile
    {
        public PostMapperProfile()
        {
            CreateMap<Post, PostModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? 0))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.Hashtags, opt => opt.MapFrom(src => src.PostHashtags != null ? src.PostHashtags.Select(ph => ph.Hashtag != null ? ph.Hashtag.Name : "").Where(n => !string.IsNullOrEmpty(n)).ToList() : new List<string>()))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.PostImages != null ? src.PostImages.Select(pi => pi.ImgUrl).Where(url => !string.IsNullOrEmpty(url)).ToList() : new List<string>()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedDate));
        }
    }
}
