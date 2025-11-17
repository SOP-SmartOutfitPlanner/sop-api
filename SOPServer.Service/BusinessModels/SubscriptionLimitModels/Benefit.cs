using SOPServer.Repository.Enums;

namespace SOPServer.Service.BusinessModels.SubscriptionLimitModels
{
    public class Benefit
    {
        public FeatureCode FeatureCode { get; set; }
        public int Usage { get; set; }  // In Plan: LIMIT (max credits), In UserSubscription: REMAINING credits
        public BenefitType BenefitType { get; set; }

        public Benefit()
        {
        }

        public Benefit(FeatureCode featureCode, int usage, BenefitType benefitType)
        {
            FeatureCode = featureCode;
            Usage = usage;
            BenefitType = benefitType;
        }
    }
}
