using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SOPServer.Service.BusinessModels.GeminiModels
{
    /// <summary>
    /// Model for structured item query criteria from Gemini AI
    /// Used to query the database for items matching specific attributes
    /// </summary>
    public class ItemQueryStructure
    {
        /// <summary>
        /// List of item query criteria, one for each clothing piece needed in the outfit
        /// </summary>
        [JsonPropertyName("items")]
        public List<ItemCriteria> Items { get; set; } = new List<ItemCriteria>();
    }

    /// <summary>
    /// Criteria for a single item in the outfit
    /// </summary>
    public class ItemCriteria
    {
        /// <summary>
        /// Category of the item (e.g., "shirt", "pants", "shoes")
        /// </summary>
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        /// <summary>
        /// List of suitable styles for this item (e.g., ["Casual", "Smart Casual"])
        /// </summary>
        [JsonPropertyName("styles")]
        public List<string>? Styles { get; set; }

        /// <summary>
        /// List of suitable occasions for this item (e.g., ["Work", "Party"])
        /// </summary>
        [JsonPropertyName("occasions")]
        public List<string>? Occasions { get; set; }

        /// <summary>
        /// List of suitable seasons for this item (e.g., ["Spring", "Summer"])
        /// </summary>
        [JsonPropertyName("seasons")]
        public List<string>? Seasons { get; set; }

        /// <summary>
        /// Preferred color names or general color families (e.g., ["blue", "navy", "dark colors"])
        /// </summary>
        [JsonPropertyName("colors")]
        public List<string>? Colors { get; set; }

        /// <summary>
        /// Weather suitability (e.g., "warm", "cold", "mild")
        /// </summary>
        [JsonPropertyName("weatherSuitable")]
        public string? WeatherSuitable { get; set; }

        /// <summary>
        /// Description of the item for context (e.g., "A casual button-up shirt")
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
