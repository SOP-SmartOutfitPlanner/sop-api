using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class RemoveSeasonFromItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Season ID is required")]
        public long SeasonId { get; set; }
    }

    public class RemoveSeasonFromItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public long RemovedSeasonId { get; set; }
        public string RemovedSeasonName { get; set; }
    }
}
