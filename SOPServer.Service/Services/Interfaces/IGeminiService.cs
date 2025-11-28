using CSharpToJsonSchema;
using GenerativeAI;
using GenerativeAI.Tools;
using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.QDrantModels;
using SOPServer.Service.BusinessModels.UserModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        Task<List<string>> OutfitSuggestion(string occasion, string usercharacteristic, string? weather = null);
        Task<OutfitSelectionModel> ChooseOutfit(string occasion, string usercharacteristic, List<string> searchResults, string? weather = null);
        GenerativeModel CreateSuggestionModel(QuickTools? tools = null);
        Task<List<long>> ItemCharacteristicSuggestion(string json, string occasion, string usercharacteristic);
        Task<OutfitSelectionModel> ChooseOutfitV2(string json, string occasion, string usercharacteristic, string? weather = null);
    }
}
