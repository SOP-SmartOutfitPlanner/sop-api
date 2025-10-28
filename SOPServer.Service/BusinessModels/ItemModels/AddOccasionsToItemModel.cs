using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class AddOccasionsToItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Occasion IDs are required")]
        [MinLength(1, ErrorMessage = "At least one occasion ID is required")]
        public List<long> OccasionIds { get; set; } = new List<long>();
    }

    public class AddedOccasionModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class AddOccasionsToItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public List<AddedOccasionModel> AddedOccasions { get; set; } = new List<AddedOccasionModel>();
    }
}
