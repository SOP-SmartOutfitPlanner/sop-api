using SOPServer.Service.BusinessModels.ItemModels;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    public class OutfitSuggestionModel
    {
        public List<SuggestedItemModel> SuggestedItems { get; set; } = new List<SuggestedItemModel>();
        public string Reason { get; set; }
    }

    public class SuggestedItemModel
    {
        public long ItemId { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string Color { get; set; }
        public string ImgUrl { get; set; }
        public string AiDescription { get; set; }
        public float MatchScore { get; set; }
    }

    public class OutfitSelectionModel
    {
        [JsonPropertyName("itemIds")]
        public List<long> ItemIds { get; set; }
        [JsonPropertyName("reason")]
        public string Reason { get; set; }
    }
}
