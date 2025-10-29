using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.StyleModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class ItemMapperProfile : Profile
    {
        public ItemMapperProfile()
        {
            CreateMap<Item, ItemModel>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : null))
                .ForMember(dest => dest.Occasions, opt => opt.MapFrom(src =>
                    src.ItemOccasions != null
                        ? src.ItemOccasions
                            .Where(io => !io.IsDeleted && io.Occasion != null)
                            .Select(io => new OccasionItemModel
                            {
                                Id = io.Occasion.Id,
                                Name = io.Occasion.Name
                            })
                            .ToList()
                        : new List<OccasionItemModel>()))
                .ForMember(dest => dest.Seasons, opt => opt.MapFrom(src =>
                    src.ItemSeasons != null
                        ? src.ItemSeasons
                            .Where(itemSeason => !itemSeason.IsDeleted && itemSeason.Season != null)
                            .Select(itemSeason => new SeasonItemModel
                            {
                                Id = itemSeason.Season.Id,
                                Name = itemSeason.Season.Name
                            })
                            .ToList()
                        : new List<SeasonItemModel>()))
                .ForMember(dest => dest.Styles, opt => opt.MapFrom(src =>
                    src.ItemStyles != null
                        ? src.ItemStyles
                            .Where(itemStyle => !itemStyle.IsDeleted && itemStyle.Style != null)
                            .Select(itemStyle => new StyleItemModel
                            {
                                Id = itemStyle.Style.Id,
                                Name = itemStyle.Style.Name
                            })
                            .ToList()
                        : new List<StyleItemModel>()));

            CreateMap<ItemModel, Item>()
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore());

            CreateMap<ItemCreateModel, Item>();

            CreateMap<Pagination<Item>, Pagination<ItemModel>>()
                .ConvertUsing<PaginationConverter<Item, ItemModel>>();

            CreateMap<ItemModelAI, ItemSummaryModel>();
        }
    }
}
