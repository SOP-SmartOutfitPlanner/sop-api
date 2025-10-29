using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class AddStylesToItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Style IDs are required")]
        [MinLength(1, ErrorMessage = "At least one style ID is required")]
        public List<long> StyleIds { get; set; } = new List<long>();
    }

    public class AddedStyleModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class AddStylesToItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public List<AddedStyleModel> AddedStyles { get; set; } = new List<AddedStyleModel>();
    }
}
