using SOPServer.Repository.Enums;

namespace SOPServer.Service.BusinessModels.SubscriptionLimitModels
{
    public class Benefit
    {
        public FeatureCode FeatureCode { get; set; }
        public int Usage { get; set; }  // In Plan: LIMIT (max credits), In UserSubscription: REMAINING credits
        public ResetType ResetType { get; set; }

        public Benefit()
        {
        }

        public Benefit(FeatureCode featureCode, int usage, ResetType resetType)
        {
            FeatureCode = featureCode;
            Usage = usage;
            ResetType = resetType;
        }
    }
}
