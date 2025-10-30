using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ReplaceSeasonsForItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Season IDs are required")]
        public List<long> SeasonIds { get; set; } = new List<long>();
    }

    public class ReplaceSeasonsForItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public List<AddedSeasonModel> ReplacedSeasons { get; set; } = new List<AddedSeasonModel>();
    }
}
