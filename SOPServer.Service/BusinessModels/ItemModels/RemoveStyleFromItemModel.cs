using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class RemoveStyleFromItemModel
    {
        [Required(ErrorMessage = "Item ID is required")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "Style ID is required")]
        public long StyleId { get; set; }
    }

    public class RemoveStyleFromItemResponseModel
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public long RemovedStyleId { get; set; }
        public string RemovedStyleName { get; set; }
    }
}
