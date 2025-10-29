using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ReplaceOccasionsForItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Occasion IDs are required")]
        public List<long> OccasionIds { get; set; } = new List<long>();
    }

    public class ReplaceOccasionsForItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public List<AddedOccasionModel> ReplacedOccasions { get; set; } = new List<AddedOccasionModel>();
    }
}
