using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.SubscriptionPlanModels;
using SOPServer.Service.BusinessModels.SubscriptionLimitModels;
using System.Text.Json;

namespace SOPServer.Service.Mappers
{
    public class SubscriptionPlanMapperProfile : Profile
    {
        public SubscriptionPlanMapperProfile()
        {
            // Entity -> Model (Deserialize JSON string to List<Benefit>)
            CreateMap<SubscriptionPlan, SubscriptionPlanModel>()
                .ForMember(dest => dest.BenefitLimit, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.BenefitLimit)
                        ? new List<Benefit>()
                        : JsonSerializer.Deserialize<List<Benefit>>(src.BenefitLimit, (JsonSerializerOptions?)null) ?? new List<Benefit>()));

            // RequestModel -> Entity (Serialize List<Benefit> to JSON string)
            CreateMap<SubscriptionPlanRequestModel, SubscriptionPlan>()
                .ForMember(dest => dest.BenefitLimit, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(src.BenefitLimit, (JsonSerializerOptions?)null)));
        }
    }
}
