using AutoMapper;
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
        }
    }
}
