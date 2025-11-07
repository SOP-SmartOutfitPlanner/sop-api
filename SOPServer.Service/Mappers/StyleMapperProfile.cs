using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.StyleModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class StyleMapperProfile : Profile
    {
        public StyleMapperProfile()
        {
            CreateMap<Style, StyleModel>();

            CreateMap<StyleModel, Style>();

            CreateMap<StyleItemModel, Style>().ReverseMap();

            CreateMap<StyleUpdateModel, Style>();

            CreateMap<StyleCreateModel, Style>();

            CreateMap<Style, StyleDetailModel>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.ItemStyles != null ? src.ItemStyles.Select(x => x.Item) : new List<Item>()));

            CreateMap<Pagination<Style>, Pagination<StyleModel>>()
                .ConvertUsing<PaginationConverter<Style, StyleModel>>();
        }
    }
}
