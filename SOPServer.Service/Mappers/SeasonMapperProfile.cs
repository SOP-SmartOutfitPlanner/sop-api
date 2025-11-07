using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.StyleModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class SeasonMapperProfile : Profile
    {
        public SeasonMapperProfile()
        {
            CreateMap<Season, SeasonModel>();

            CreateMap<SeasonModel, Season>();

            CreateMap<SeasonItemModel, Season>().ReverseMap();

            CreateMap<Season, SeasonDetailModel>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.ItemSeasons != null ? src.ItemSeasons.Select(x => x.Item) : new List<Item>()));

            CreateMap<SeasonUpdateModel, Season>();

            CreateMap<SeasonCreateModel, Season>();

            CreateMap<Pagination<Season>, Pagination<SeasonModel>>()
                .ConvertUsing<PaginationConverter<Season, SeasonModel>>();
        }
    }
}
