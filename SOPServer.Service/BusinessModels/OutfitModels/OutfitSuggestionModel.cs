using SOPServer.Service.BusinessModels.ItemModels;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    public class OutfitSuggestionResponseModel
    {
        public SelectedOutfitModel SelectedOutfit { get; set; }
        public string Explanation { get; set; }
    }

    public class SelectedOutfitModel
    {
        public List<OutfitItemDetailModel> Items { get; set; } = new List<OutfitItemDetailModel>();
    }

    public class OutfitItemDetailModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string ImgUrl { get; set; }
    }

    // Internal model for Gemini outfit generation
    public class GeminiOutfitGenerationModel
    {
        public string OutfitType { get; set; }
        public string Description { get; set; }
        public List<OutfitItemDescriptionModel> Items { get; set; } = new List<OutfitItemDescriptionModel>();
        public string Occasion { get; set; }
        public List<string> ColorPalette { get; set; } = new List<string>();
    }

    public class OutfitItemDescriptionModel
    {
        public string Category { get; set; }
        public string Description { get; set; }
    }

    // Internal model for Gemini outfit selection
    public class GeminiOutfitSelectionRequest
    {
        public string UserContext { get; set; }
        public List<CategoryItemCandidates> Candidates { get; set; } = new List<CategoryItemCandidates>();
    }

    public class CategoryItemCandidates
    {
        public string Category { get; set; }
        public List<ItemCandidateModel> Items { get; set; } = new List<ItemCandidateModel>();
    }

    public class ItemCandidateModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public string Pattern { get; set; }
        public string Fabric { get; set; }
    }

    // Gemini response for final outfit selection
    public class GeminiOutfitSelectionResponse
    {
        public List<long> SelectedItemIds { get; set; } = new List<long>();
        public string Explanation { get; set; }
    }
}
