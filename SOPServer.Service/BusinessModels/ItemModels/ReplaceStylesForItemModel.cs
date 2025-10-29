using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ReplaceStylesForItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Style IDs are required")]
        public List<long> StyleIds { get; set; } = new List<long>();
    }

    public class ReplaceStylesForItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public List<AddedStyleModel> ReplacedStyles { get; set; } = new List<AddedStyleModel>();
    }
}
