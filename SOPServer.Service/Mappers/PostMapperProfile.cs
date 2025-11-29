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

            CreateMap<Item, PostItemDetailModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.ImgUrl, opt => opt.MapFrom(src => src.ImgUrl))
                .ForMember(dest => dest.AiDescription, opt => opt.MapFrom(src => src.AiDescription))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted));

            CreateMap<Outfit, PostOutfitDetailModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
                    src.OutfitItems != null
                    ? src.OutfitItems
                        .Where(oi => !oi.IsDeleted && oi.Item != null)
                        .Select(oi => new PostItemDetailModel
                        {
                            Id = oi.Item.Id,
                            Name = oi.Item.Name,
                            CategoryId = oi.Item.CategoryId,
                            CategoryName = oi.Item.Category != null ? oi.Item.Category.Name : string.Empty,
                            Color = oi.Item.Color,
                            Brand = oi.Item.Brand,
                            ImgUrl = oi.Item.ImgUrl,
                            AiDescription = oi.Item.AiDescription,
                            IsDeleted = oi.Item.IsDeleted
                        })
                        .ToList()
                    : new List<PostItemDetailModel>()));

            CreateMap<Post, PostModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? 0))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User.AvtUrl != null ? src.User.AvtUrl : null))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.User.Role.ToString()))
                .ForMember(dest => dest.IsHidden, opt => opt.MapFrom(src => src.IsHidden))
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
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
                    src.PostItems != null
                    ? src.PostItems
                        .Where(pi => !pi.IsDeleted && pi.Item != null)
                        .Select(pi => pi.Item)
                        .ToList()
                    : null))
                .ForMember(dest => dest.Outfit, opt => opt.MapFrom(src =>
                    src.PostOutfits != null
                    ? src.PostOutfits
                        .Where(po => !po.IsDeleted && po.Outfit != null)
                        .Select(po => po.Outfit)
                        .FirstOrDefault()
                    : null))
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
