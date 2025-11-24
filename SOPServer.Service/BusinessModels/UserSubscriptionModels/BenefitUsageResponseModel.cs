using SOPServer.Repository.Enums;

namespace SOPServer.Service.BusinessModels.UserSubscriptionModels
{
    public class BenefitUsageResponseModel
    {
        public FeatureCode FeatureCode { get; set; }
        public int Usage { get; set; }  
        public int Limit { get; set; }  
        public BenefitType BenefitType { get; set; }
    }
}
