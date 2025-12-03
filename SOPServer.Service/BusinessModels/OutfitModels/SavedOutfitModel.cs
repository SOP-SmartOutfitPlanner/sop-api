using SOPServer.Service.BusinessModels.ItemModels;
using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    /// <summary>
    /// Represents a saved outfit with information about its source (Post or Collection)
    /// </summary>
    public class SavedOutfitModel
    {
        public long Id { get; set; }
        public long OutfitId { get; set; }
        public string? OutfitName { get; set; }
        public string? OutfitDescription { get; set; }
        public long UserId { get; set; }
        public DateTime SavedDate { get; set; }

        /// <summary>
        /// Source type: "Post" or "Collection"
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the source (PostId or CollectionId)
        /// </summary>
        public long SourceId { get; set; }

        /// <summary>
        /// Title or body of the source (Post body or Collection title)
        /// </summary>
        public string? SourceTitle { get; set; }

        /// <summary>
        /// User ID of the source owner
        /// </summary>
        public long? SourceOwnerId { get; set; }

        /// <summary>
        /// Display name of the source owner
        /// </summary>
        public string? SourceOwnerDisplayName { get; set; }

        /// <summary>
        /// List of items in the outfit with full details
        /// </summary>
        public List<OutfitItemModel> Items { get; set; } = new List<OutfitItemModel>();
    }

    /// <summary>
    /// Filter model for querying saved outfits
    /// </summary>
    public class SavedOutfitFilterModel
    {
        /// <summary>
        /// Filter by source type: "Post", "Collection", or null for both
        /// </summary>
        public string? SourceType { get; set; }

        /// <summary>
        /// Search in outfit name, description, or source title
        /// </summary>
        public string? Search { get; set; }
    }
}
