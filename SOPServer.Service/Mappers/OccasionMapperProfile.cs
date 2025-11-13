using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.OutfitCalendarModels;

namespace SOPServer.Service.Mappers
{
    public class OccasionMapperProfile : Profile
    {
        public OccasionMapperProfile()
        {
            CreateMap<Occasion, OccasionModel>();

            CreateMap<Occasion, OccasionItemModel>().ReverseMap();

            CreateMap<OccasionModel, Occasion>();

            CreateMap<OccasionUpdateModel, Occasion>();

            CreateMap<OccasionCreateModel, Occasion>();

            CreateMap<Pagination<Occasion>, Pagination<OccasionModel>>()
                .ConvertUsing<PaginationConverter<Occasion, OccasionModel>>();

            // Map Pagination<OutfitUsageHistory> to Pagination<OutfitCalendarModel>
            CreateMap<Pagination<OutfitUsageHistory>, Pagination<OutfitCalendarModel>>()
                .ConvertUsing((src, dest, context) =>
                {
                    var items = context.Mapper.Map<List<OutfitCalendarModel>>(src.ToList());
                    return new Pagination<OutfitCalendarModel>(items, src.TotalCount, src.CurrentPage, src.PageSize);
                });

            // Map OutfitUsageHistory entity to OutfitCalendarModel
            CreateMap<OutfitUsageHistory, OutfitCalendarModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.OutfitId, opt => opt.MapFrom(src => src.OutfitId))
                .ForMember(dest => dest.OutfitName, opt => opt.MapFrom(src => src.Outfit != null ? src.Outfit.Name ?? "Unnamed Outfit" : "Unknown"))
                .ForMember(dest => dest.UserOccasionId, opt => opt.MapFrom(src => src.UserOccassionId))
                .ForMember(dest => dest.UserOccasionName, opt => opt.MapFrom(src => src.UserOccasion != null ? src.UserOccasion.Name : null))
                .ForMember(dest => dest.IsDaily, opt => opt.MapFrom(src => src.UserOccasion != null && src.UserOccasion.Name == "Daily"))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate));

            // Map OutfitUsageHistory entity to OutfitCalendarDetailedModel
            CreateMap<OutfitUsageHistory, OutfitCalendarDetailedModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.OutfitId, opt => opt.MapFrom(src => src.OutfitId))
                .ForMember(dest => dest.Outfit, opt => opt.MapFrom(src => src.Outfit))
                .ForMember(dest => dest.UserOccasionId, opt => opt.MapFrom(src => src.UserOccassionId))
                .ForMember(dest => dest.UserOccasion, opt => opt.MapFrom(src => src.UserOccasion))
                .ForMember(dest => dest.IsDaily, opt => opt.MapFrom(src => src.UserOccasion != null && src.UserOccasion.Name == "Daily"))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate));

            // Note: OutfitCalendarCreateModel now contains multiple OutfitIds
            // Calendar entries are created manually in the service
        }
    }
}
