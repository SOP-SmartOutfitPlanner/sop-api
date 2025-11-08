using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.CategoryModels;
using SOPServer.Service.BusinessModels.PostModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class PostMapperProfile : Profile
    {
        public PostMapperProfile()
        {
            CreateMap<Hashtag, HashtagModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            CreateMap<Post, PostModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? 0))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.Hashtags, opt => opt.MapFrom(src => 
                    src.PostHashtags != null 
                    ? src.PostHashtags
                        .Where(ph => !ph.IsDeleted && ph.Hashtag != null)
                        .Select(ph => new HashtagModel 
                        { 
                            Id = ph.Hashtag.Id, 
                            Name = ph.Hashtag.Name 
                        })
                        .ToList() 
                    : new List<HashtagModel>()))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.PostImages != null ? src.PostImages.Where(pi => !pi.IsDeleted).Select(pi => pi.ImgUrl).Where(url => !string.IsNullOrEmpty(url)).ToList() : new List<string>()))
                .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.LikePosts != null ? src.LikePosts.Count(lp => !lp.IsDeleted) : 0))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.CommentPosts != null ? src.CommentPosts.Count(lp => !lp.IsDeleted) : 0))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedDate));

            CreateMap<Post, NewsfeedPostModel>()
                .IncludeBase<Post, PostModel>()
                .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.LikePosts != null ? src.LikePosts.Count(lp => !lp.IsDeleted) : 0))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.CommentPosts != null ? src.CommentPosts.Count(cp => !cp.IsDeleted) : 0))
                .ForMember(dest => dest.IsLikedByUser, opt => opt.Ignore()) // Set manually in service
                .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvtUrl : null))
                .ForMember(dest => dest.RankingScore, opt => opt.Ignore()); // Set manually in service


            CreateMap<Pagination<Post>, Pagination<PostModel>>()
                .ConvertUsing<PaginationConverter<Post, PostModel>>();
        }
    }
}
