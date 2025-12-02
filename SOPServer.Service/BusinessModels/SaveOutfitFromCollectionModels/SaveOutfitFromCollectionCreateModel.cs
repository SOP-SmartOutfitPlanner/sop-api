using System;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.SaveOutfitFromCollectionModels
{
    public class SaveOutfitFromCollectionCreateModel
    {
        [Required(ErrorMessage = "OutfitId is required")]
        public long OutfitId { get; set; }

        [Required(ErrorMessage = "CollectionId is required")]
        public long CollectionId { get; set; }
    }
}
