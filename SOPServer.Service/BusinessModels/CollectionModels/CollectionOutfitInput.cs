using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.CollectionModels
{
    public class CollectionOutfitInput
    {
        [Required(ErrorMessage = "Outfit ID is required")]
        public long OutfitId { get; set; }

        [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }
    }
}
