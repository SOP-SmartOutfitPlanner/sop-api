using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.AISettingModels;
using SOPServer.Service.BusinessModels.CategoryModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.OnboardingModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.UserModels;
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

            // Outfit mappings
            CreateMap<Outfit, OutfitModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? 0))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.IsFavorite, opt => opt.MapFrom(src => src.isFavorite))
                .ForMember(dest => dest.IsUsed, opt => opt.MapFrom(src => src.isUsed));

            CreateMap<Outfit, OutfitDetailedModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? 0))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.IsFavorite, opt => opt.MapFrom(src => src.isFavorite))
                .ForMember(dest => dest.IsUsed, opt => opt.MapFrom(src => src.isUsed))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
                    src.OutfitItems != null
                        ? src.OutfitItems.Where(oi => oi.Item != null).Select(oi => oi.Item).ToList()
                        : new List<Item>()));

            CreateMap<Item, OutfitItemModel>()
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId ?? 0))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));

            // Season mappings
            CreateMap<Season, SeasonModel>();

            CreateMap<SeasonModel, Season>();

            CreateMap<Season, SeasonDetailModel>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.ItemSeasons != null ? src.ItemSeasons.Select(x => x.Item) : new List<Item>()));

            CreateMap<SeasonUpdateModel, Season>();

            CreateMap<SeasonCreateModel, Season>();

            CreateMap<Pagination<Season>, Pagination<SeasonModel>>().ConvertUsing<PaginationConverter<Season, SeasonModel>>();
            CreateMap<ItemModel, Item>().ReverseMap();
            CreateMap<ItemCreateModel, Item>();

            /////////////////////////////////////////////////////
            ///                 USER MAPPING                 ///
            ///////////////////////////////////////////////////

            CreateMap<OnboardingRequestModel, User>();

            // User List mapping
            CreateMap<User, UserListModel>()
                .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Job != null ? src.Job.Name : null))
                .ForMember(dest => dest.UserStyles, opt => opt.MapFrom(src => src.UserStyles != null ? src.UserStyles.ToList() : new List<UserStyle>()));

            CreateMap<Pagination<User>, Pagination<UserListModel>>().ConvertUsing<PaginationConverter<User, UserListModel>>();

            // User Profile mapping
            CreateMap<User, UserProfileModel>()
                .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.Job != null ? src.Job.Name : null))
                .ForMember(dest => dest.JobDescription, opt => opt.MapFrom(src => src.Job != null ? src.Job.Description : null))
                .ForMember(dest => dest.UserStyles, opt => opt.MapFrom(src => src.UserStyles != null ? src.UserStyles.ToList() : new List<UserStyle>()));

            CreateMap<UserStyle, UserStyleModel>()
                .ForMember(dest => dest.StyleId, opt => opt.MapFrom(src => src.StyleId ?? 0))
                .ForMember(dest => dest.StyleName, opt => opt.MapFrom(src => src.Style != null ? src.Style.Name : string.Empty))
                .ForMember(dest => dest.StyleDescription, opt => opt.MapFrom(src => src.Style != null ? src.Style.Description : string.Empty));

            // Occasion mappings
            CreateMap<Occasion, OccasionModel>();
            CreateMap<OccasionModel, Occasion>();
            CreateMap<OccasionUpdateModel, Occasion>();
            CreateMap<OccasionCreateModel, Occasion>();
            CreateMap<Pagination<Occasion>, Pagination<OccasionModel>>().ConvertUsing<PaginationConverter<Occasion, OccasionModel>>();

            //AISetting
            CreateMap<AISetting, AISettingModel>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString())).ReverseMap();

            CreateMap<AISettingRequestModel, AISetting>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type));
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
