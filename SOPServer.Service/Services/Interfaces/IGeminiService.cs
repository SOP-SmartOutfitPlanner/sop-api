using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<ImageValidation> ImageValidation(string base64Image, string mimeType);
        Task<ItemModelAI?> ImageGenerateContent(string base64Image, string mimeType, string prompt);
        Task<List<float>?> EmbeddingText(string textEmbeeding);
        Task<CategoryItemAnalysisModel?> AnalyzingCategory(string base64Image, string mimeType, string finalPrompt);
        Task<List<string>> GenerateOutfitSuggestItem(string userCharacteristic, string occasion);
        Task<OutfitSelectionModel> SelectOutfitFromItems(string userCharacteristic, string occasion, List<ItemModel> availableItems);
    }
}
