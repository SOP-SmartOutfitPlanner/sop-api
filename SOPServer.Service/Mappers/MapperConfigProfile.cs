using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.CategoryModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.PostModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class MapperConfigProfile : Profile
    {
        public MapperConfigProfile()
        {
            CreateMap<Item, ItemModel>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : null));

            CreateMap<ItemModel, Item>()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore());

            // Map creation model to entity
            CreateMap<ItemCreateModel, Item>();

            CreateMap<Pagination<Item>, Pagination<ItemModel>>().ConvertUsing<PaginationConverter<Item, ItemModel>>();


            CreateMap<ItemModelAI, ItemSummaryModel>();

            // Category mappings
            CreateMap<Category, CategoryModel>()
                .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null));

            CreateMap<CategoryModel, Category>();

            CreateMap<CategoryUpdateModel, Category>();

            CreateMap<CategoryCreateModel, Category>();

            CreateMap<Pagination<Category>, Pagination<CategoryModel>>().ConvertUsing<PaginationConverter<Category, CategoryModel>>();

            // Post mappings
            CreateMap<Post, PostModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? 0))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.Hashtags, opt => opt.MapFrom(src => src.PostHashtags != null ? src.PostHashtags.Select(ph => ph.Hashtag != null ? ph.Hashtag.Name : "").Where(n => !string.IsNullOrEmpty(n)).ToList() : new List<string>()))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.PostImages != null ? src.PostImages.Select(pi => pi.ImgUrl).Where(url => !string.IsNullOrEmpty(url)).ToList() : new List<string>()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedDate));
        }

        public class PaginationConverter<TSource, TDestination> : ITypeConverter<Pagination<TSource>, Pagination<TDestination>>
        {
            public Pagination<TDestination> Convert(Pagination<TSource> source, Pagination<TDestination> destination, ResolutionContext context)
            {
                var mappedItems = context.Mapper.Map<List<TDestination>>(source);
                return new Pagination<TDestination>(mappedItems, source.TotalCount, source.CurrentPage, source.PageSize);
            }
        }
    }
}
