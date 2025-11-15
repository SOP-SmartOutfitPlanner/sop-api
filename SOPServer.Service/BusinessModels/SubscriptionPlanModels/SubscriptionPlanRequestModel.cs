using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.SubscriptionPlanModels
{
    public class SubscriptionPlanRequestModel
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, long.MaxValue, ErrorMessage = "Price must be a positive number")]
        public long Price { get; set; }

        public string? BenefitLimit { get; set; }
    }
}
