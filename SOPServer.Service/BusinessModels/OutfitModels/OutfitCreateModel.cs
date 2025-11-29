using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    public class OutfitCreateModel
    {
        /// <example>Summer Beach Outfit</example>
        public string? Name { get; set; }

        /// <example>Light and breezy outfit perfect for sunny beach days</example>
        public string? Description { get; set; }

        /// <example>[1, 2, 3]</example>
        public List<long> ItemIds { get; set; } = new List<long>();
    }

    public class OutfitUpdateModel
    {
        /// <example>Updated Summer Outfit</example>
        public string? Name { get; set; }

        /// <example>Updated description for cooler weather</example>
        public string? Description { get; set; }

        public List<long>? ItemIds { get; set; }
    }

    public class MassOutfitCreateModel
    {
        /// <summary>
        /// List of outfits to create
        /// </summary>
        public List<OutfitCreateModel> Outfits { get; set; } = new List<OutfitCreateModel>();
    }

    public class MassOutfitCreateResultModel
    {
        public int TotalRequested { get; set; }
        public int TotalCreated { get; set; }
        public int TotalFailed { get; set; }
        public List<OutfitCreateSuccessModel> CreatedOutfits { get; set; } = new List<OutfitCreateSuccessModel>();
        public List<OutfitCreateFailureModel> FailedOutfits { get; set; } = new List<OutfitCreateFailureModel>();
    }

    public class OutfitCreateSuccessModel
    {
        public int Index { get; set; }
        public long OutfitId { get; set; }
        public string? Name { get; set; }
    }

    public class OutfitCreateFailureModel
    {
        public int Index { get; set; }
        public string? Name { get; set; }
        public string Error { get; set; }
    }
}
