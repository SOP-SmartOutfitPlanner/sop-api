using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.UserSubscriptionModels
{
    public class PurchaseSubscriptionRequestModel
    {
        [Required(ErrorMessage = "SubscriptionPlanId is required")]
        public long SubscriptionPlanId { get; set; }
    }
}
