using AutoMapper;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.OutfitCalendarModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.UserOccasionModels;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Mappers
{
    public class UserOccasionMapperProfile : Profile
    {
        public UserOccasionMapperProfile()
        {
            // Map Pagination<UserOccasion> to Pagination<UserOccasionModel>
            CreateMap<Pagination<UserOccasion>, Pagination<UserOccasionModel>>()
                .ConvertUsing((src, dest, context) =>
                {
                    var items = context.Mapper.Map<List<UserOccasionModel>>(src.ToList());
                    return new Pagination<UserOccasionModel>(items, src.TotalCount, src.CurrentPage, src.PageSize);
                });

            // Map UserOccasion entity to UserOccasionModel
            CreateMap<UserOccasion, UserOccasionModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.OccasionId, opt => opt.MapFrom(src => src.OccasionId))
                .ForMember(dest => dest.OccasionName, opt => opt.MapFrom(src => src.Occasion != null ? src.Occasion.Name : null))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DateOccasion, opt => opt.MapFrom(src => src.DateOccasion))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.WeatherSnapshot, opt => opt.MapFrom(src => src.WeatherSnapshot))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate));

            // Map UserOccasion entity to UserOccasionDetailedModel
            CreateMap<UserOccasion, UserOccasionDetailedModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User != null ? src.User.DisplayName : "Unknown"))
                .ForMember(dest => dest.OccasionId, opt => opt.MapFrom(src => src.OccasionId))
                .ForMember(dest => dest.Occasion, opt => opt.MapFrom(src => src.Occasion))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DateOccasion, opt => opt.MapFrom(src => src.DateOccasion))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.WeatherSnapshot, opt => opt.MapFrom(src => src.WeatherSnapshot))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate))
                .ForMember(dest => dest.PlannedOutfit, opt => opt.Ignore()) // Will be filled manually in service
                .ForMember(dest => dest.HasOutfitPlanned, opt => opt.Ignore()); // Will be filled manually in service

            // Map UserOccasionCreateModel to UserOccasion entity
            CreateMap<UserOccasionCreateModel, UserOccasion>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Occasion, opt => opt.Ignore())
                .ForMember(dest => dest.OutfitUsageHistories, opt => opt.Ignore());

            // Map UserOccasion entity to OutfitCalendarGroupedModel
            CreateMap<UserOccasion, OutfitCalendarGroupedModel>()
                .ForMember(dest => dest.UserOccasion, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.IsDaily, opt => opt.MapFrom(src => src.Name == "Daily"))
                .ForMember(dest => dest.Outfits, opt => opt.MapFrom((src, dest, destMember, context) =>
                    src.OutfitUsageHistories != null
                        ? src.OutfitUsageHistories
                            .Where(ouh => !ouh.IsDeleted && ouh.Outfit != null)
                            .Select(ouh => new OutfitCalendarItemModel
                            {
                                CalendarId = ouh.Id,
                                OutfitId = ouh.OutfitId,
                                OutfitName = ouh.Outfit.Name ?? "Unnamed Outfit",
                                OutfitDetails = context.Mapper.Map<OutfitModel>(ouh.Outfit),
                                CreatedDate = ouh.CreatedDate
                            })
                            .ToList()
                        : new List<OutfitCalendarItemModel>()));
        }
    }
}
