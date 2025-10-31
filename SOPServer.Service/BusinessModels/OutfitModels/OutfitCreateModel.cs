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
}
