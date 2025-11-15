namespace SOPServer.Service.BusinessModels.SubscriptionPlanModels
{
    public class SubscriptionPlanModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public long Price { get; set; }
        public string? BenefitLimit { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
