using AutoMapper;
using SOPServer.Repository.Entities;
using SOPServer.Service.BusinessModels.SubscriptionPlanModels;

namespace SOPServer.Service.Mappers
{
    public class SubscriptionPlanMapperProfile : Profile
    {
        public SubscriptionPlanMapperProfile()
        {
            CreateMap<SubscriptionPlan, SubscriptionPlanModel>();
            CreateMap<SubscriptionPlanRequestModel, SubscriptionPlan>();
        }
    }
}
