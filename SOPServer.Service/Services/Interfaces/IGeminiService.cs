using CSharpToJsonSchema;
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
    [GenerateJsonSchema(GoogleFunctionTool = true, MeaiFunctionTool = true)]
    public interface IGeminiService
    {
        Task<ImageValidation> ImageValidation(string base64Image, string mimeType, CancellationToken cancellationToken = default);
        Task<ItemModelAI?> ImageGenerateContent(string base64Image, string mimeType, string prompt, CancellationToken cancellationToken = default);
        [Description("Embeeding a text")]
        Task<List<float>?> EmbeddingText(string textEmbeeding, CancellationToken cancellationToken = default);
        Task<CategoryItemAnalysisModel?> AnalyzingCategory(string base64Image, string mimeType, string finalPrompt, CancellationToken cancellationToken = default);
        //Task OutfitSuggestion(string occasion, string usercharacteristic, long userId, QuickTools tools);
        Task<List<string>> OutfitSuggestion(string occasion, string usercharacteristic, CancellationToken cancellationToken = default);
        Task<OutfitSelectionModel> ChooseOutfit(string occasion, string usercharacteristic, List<QDrantSearchModels> items, CancellationToken cancellationToken = default);
    }
}
