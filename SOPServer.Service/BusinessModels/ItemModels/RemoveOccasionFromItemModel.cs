using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class RemoveOccasionFromItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Occasion ID is required")]
        public long OccasionId { get; set; }
    }

    public class RemoveOccasionFromItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public long RemovedOccasionId { get; set; }
        public string RemovedOccasionName { get; set; }
    }
}
