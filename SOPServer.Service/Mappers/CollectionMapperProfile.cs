using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.CollectionModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class CollectionMapperProfile : Profile
    {
        public CollectionMapperProfile()
        {
            CreateMap<Pagination<Collection>, Pagination<CollectionModel>>()
                .ConvertUsing((src, dest, context) =>
                {
                    var items = context.Mapper.Map<List<CollectionModel>>(src.ToList());
                    return new Pagination<CollectionModel>(items, src.TotalCount, src.CurrentPage, src.PageSize);
                });

            CreateMap<Pagination<Collection>, Pagination<CollectionDetailedModel>>()
                .ConvertUsing((src, dest, context) =>
                {
                    var items = context.Mapper.Map<List<CollectionDetailedModel>>(src.ToList());
                    return new Pagination<CollectionDetailedModel>(items, src.TotalCount, src.CurrentPage, src.PageSize);
                });

            CreateMap<Collection, CollectionModel>()
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.OutfitCount, opt => opt.MapFrom(src => 
                    src.CollectionOutfits != null ? src.CollectionOutfits.Count(co => !co.IsDeleted) : 0));

            CreateMap<Collection, CollectionDetailedModel>()
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.Outfits, opt => opt.MapFrom(src => 
                    src.CollectionOutfits != null 
                        ? src.CollectionOutfits.Where(co => !co.IsDeleted && co.Outfit != null).ToList() 
                        : new List<CollectionOutfit>()));

            CreateMap<CollectionOutfit, CollectionOutfitModel>();

            CreateMap<Outfit, OutfitInCollectionModel>()
                .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => 
                    src.OutfitItems != null ? src.OutfitItems.Count(oi => !oi.IsDeleted) : 0))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src =>
                    src.OutfitItems != null && src.OutfitItems.Any()
                        ? src.OutfitItems.Where(oi => oi.Item != null && !oi.IsDeleted).Select(oi => oi.Item).ToList()
                        : new List<Item>()));
        }
    }
}
