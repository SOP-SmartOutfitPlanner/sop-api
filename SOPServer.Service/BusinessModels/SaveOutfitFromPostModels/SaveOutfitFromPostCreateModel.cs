using System;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.SaveOutfitFromPostModels
{
    public class SaveOutfitFromPostCreateModel
    {
        [Required(ErrorMessage = "OutfitId is required")]
        public long OutfitId { get; set; }

        [Required(ErrorMessage = "PostId is required")]
        public long PostId { get; set; }
    }
}
