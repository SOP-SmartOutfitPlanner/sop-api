using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.OutfitModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class OutfitMapperProfile : Profile
    {
        public OutfitMapperProfile()
        {
            // Map Pagination<Outfit> to Pagination<OutfitModel>
            CreateMap<Pagination<Outfit>, Pagination<OutfitModel>>()
                .ConvertUsing((src, dest, context) =>
                {
                    var items = context.Mapper.Map<List<OutfitModel>>(src.ToList());
                    return new Pagination<OutfitModel>(items, src.TotalCount, src.CurrentPage, src.PageSize);
                });

            CreateMap<Outfit, OutfitModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.IsFavorite, opt => opt.MapFrom(src => src.IsFavorite))
                .ForMember(dest => dest.IsSaved, opt => opt.MapFrom(src => src.IsSaved))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
                    src.OutfitItems != null && src.OutfitItems.Any()
                        ? src.OutfitItems.Where(oi => oi.Item != null && !oi.IsDeleted).Select(oi => oi.Item).ToList()
                        : new List<Item>()));

            CreateMap<Outfit, OutfitDetailedModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.IsFavorite, opt => opt.MapFrom(src => src.IsFavorite))
                .ForMember(dest => dest.IsSaved, opt => opt.MapFrom(src => src.IsSaved))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
                    src.OutfitItems != null && src.OutfitItems.Any()
                        ? src.OutfitItems.Where(oi => oi.Item != null && !oi.IsDeleted).Select(oi => oi.Item).ToList()
                        : new List<Item>()));

            CreateMap<Item, OutfitItemModel>()
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId ?? 0))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.Occasions, opt => opt.MapFrom(src =>
                    src.ItemOccasions != null && src.ItemOccasions.Any()
                        ? src.ItemOccasions.Where(io => io.Occasion != null && !io.IsDeleted)
                            .Select(io => new BusinessModels.OccasionModels.OccasionItemModel
                            {
                                Id = io.Occasion.Id,
                                Name = io.Occasion.Name
                            }).ToList()
                        : new List<BusinessModels.OccasionModels.OccasionItemModel>()))
                .ForMember(dest => dest.Seasons, opt => opt.MapFrom(src =>
                    src.ItemSeasons != null && src.ItemSeasons.Any()
                        ? src.ItemSeasons.Where(iSeason => iSeason.Season != null && !iSeason.IsDeleted)
                            .Select(iSeason => new BusinessModels.SeasonModels.SeasonItemModel
                            {
                                Id = iSeason.Season.Id,
                                Name = iSeason.Season.Name
                            }).ToList()
                        : new List<BusinessModels.SeasonModels.SeasonItemModel>()))
                .ForMember(dest => dest.Styles, opt => opt.MapFrom(src =>
                    src.ItemStyles != null && src.ItemStyles.Any()
                        ? src.ItemStyles.Where(iStyle => iStyle.Style != null && !iStyle.IsDeleted)
                            .Select(iStyle => new BusinessModels.StyleModels.StyleItemModel
                            {
                                Id = iStyle.Style.Id,
                                Name = iStyle.Style.Name
                            }).ToList()
                        : new List<BusinessModels.StyleModels.StyleItemModel>()));
        }
    }
}
