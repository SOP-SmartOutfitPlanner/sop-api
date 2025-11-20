using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.SubscriptionLimitModels;
using SOPServer.Service.BusinessModels.UserSubscriptionModels;
using System.Text.Json;

namespace SOPServer.Service.Mappers
{
    public class UserSubscriptionMapperProfile : Profile
    {
        public UserSubscriptionMapperProfile()
        {
            // Entity -> Model (Deserialize JSON string to List<Benefit>)
            CreateMap<UserSubscription, UserSubscriptionModel>()
                .ForMember(dest => dest.BenefitUsage, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.BenefitUsed)
                        ? new List<Benefit>()
                        : JsonSerializer.Deserialize<List<Benefit>>(src.BenefitUsed, (JsonSerializerOptions?)null) ?? new List<Benefit>()));
        }
    }
}
