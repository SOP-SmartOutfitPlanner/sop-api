using SOPServer.Service.BusinessModels.ItemModels;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    // Request model for outfit suggestion
    public class OutfitSuggestionRequestModel
    {
        public long UserId { get; set; }
        public long? UserOccasionId { get; set; }
    }

    // Response model for outfit suggestion
    public class OutfitSuggestionResponseModel
    {
        public SelectedOutfitModel SelectedOutfit { get; set; }
        public string Explanation { get; set; }
    }

    public class SelectedOutfitModel
    {
        public OutfitItemSuggestionModel? FullBody { get; set; }
        public OutfitItemSuggestionModel? Top { get; set; }
        public OutfitItemSuggestionModel? Bottom { get; set; }
    }

    public class OutfitItemSuggestionModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string ImgUrl { get; set; }
    }

    // Context model for outfit generation
    public class OutfitGenerationContextModel
    {
        public string Gender { get; set; }
        public int? Age { get; set; }
        public string? Profession { get; set; }
        public List<string>? PreferredStyles { get; set; }
        public List<string>? FavoriteColors { get; set; }
        public List<string>? AvoidedColors { get; set; }
        public string? Location { get; set; }
        
        // Occasion details
        public string? OccasionName { get; set; }
        public string? OccasionDescription { get; set; }
        public string? WeatherSnapshot { get; set; }
    }

    // Gemini response models for outfit generation
    public class GeminiOutfitGenerationModel
    {
        public string OutfitType { get; set; } // "Full-Body" or "Separated"
        public GeminiItemDescriptionModel? FullBody { get; set; }
        public GeminiItemDescriptionModel? Top { get; set; }
        public GeminiItemDescriptionModel? Bottom { get; set; }
    }

    public class GeminiItemDescriptionModel
    {
        public string Type { get; set; }
        public List<string> ColorPalette { get; set; }
        public List<string> Style { get; set; }
        public List<string> Occasion { get; set; }
        public List<string> Season { get; set; }
        public int CategoryId { get; set; }
    }

    // Models for shortlisted items to send to Gemini
    public class ShortlistedItemsModel
    {
        public List<ShortlistedItemModel>? FullBodyItems { get; set; }
        public List<ShortlistedItemModel>? TopItems { get; set; }
        public List<ShortlistedItemModel>? BottomItems { get; set; }
    }

    public class ShortlistedItemModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string ImgUrl { get; set; }
        public string? Color { get; set; }
        public string? AiDescription { get; set; }
        public string? Pattern { get; set; }
        public string? Fabric { get; set; }
        public List<string>? Styles { get; set; }
        public List<string>? Occasions { get; set; }
        public List<string>? Seasons { get; set; }
    }

    // Gemini final selection model
    public class GeminiFinalSelectionModel
    {
        public GeminiSelectedOutfitModel SelectedOutfit { get; set; }
        public string Explanation { get; set; }
    }

    public class GeminiSelectedOutfitModel
    {
        public long? FullBodyId { get; set; }
        public long? TopId { get; set; }
        public long? BottomId { get; set; }
    }
}
