using AutoMapper;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.QDrantModels;
using SOPServer.Service.BusinessModels.StyleModels;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.SeasonModels;

namespace SOPServer.Service.Mappers
{
    public class QDrantMapperProfile : Profile
    {
        public QDrantMapperProfile()
        {
      CreateMap<ItemModel, QDrantSearchModels>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => (int)src.Id))
       .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ImgURL, opt => opt.MapFrom(src => src.ImgUrl))
                .ForMember(dest => dest.Colors, opt => opt.MapFrom(src =>
         src.Color != null
    ? new List<ColorModel> { new ColorModel { Name = src.Color } }
  : new List<ColorModel>()))
     .ForMember(dest => dest.AiDescription, opt => opt.MapFrom(src => src.AiDescription))
                .ForMember(dest => dest.WeatherSuitable, opt => opt.MapFrom(src => src.WeatherSuitable))
    .ForMember(dest => dest.Condition, opt => opt.MapFrom(src => src.Condition))
                .ForMember(dest => dest.Pattern, opt => opt.MapFrom(src => src.Pattern))
  .ForMember(dest => dest.Fabric, opt => opt.MapFrom(src => src.Fabric))
       .ForMember(dest => dest.Styles, opt => opt.MapFrom(src => src.Styles ?? new List<StyleItemModel>()))
    .ForMember(dest => dest.Occasions, opt => opt.MapFrom(src => src.Occasions ?? new List<OccasionItemModel>()))
          .ForMember(dest => dest.Seasons, opt => opt.MapFrom(src => src.Seasons ?? new List<SeasonItemModel>()))
        .ForMember(dest => dest.Confidence, opt => opt.MapFrom(src => src.AIConfidence))
           .ForMember(dest => dest.Score, opt => opt.Ignore());
        }
    }
}
