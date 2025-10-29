using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class AddSeasonsToItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Season IDs are required")]
        [MinLength(1, ErrorMessage = "At least one season ID is required")]
        public List<long> SeasonIds { get; set; } = new List<long>();
    }

    public class AddedSeasonModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class AddSeasonsToItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public List<AddedSeasonModel> AddedSeasons { get; set; } = new List<AddedSeasonModel>();
    }
}
