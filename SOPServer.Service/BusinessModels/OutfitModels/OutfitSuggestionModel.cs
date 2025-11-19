using SOPServer.Service.BusinessModels.ItemModels;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    public class OutfitSuggestionModel
    {
        public List<ItemModel> SuggestedItems { get; set; } = new List<ItemModel>();
        public string Reason { get; set; }
    }

    public class OutfitSelectionModel
    {
        [JsonPropertyName("itemIds")]
        public List<long> ItemIds { get; set; }
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }
}
