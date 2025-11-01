using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
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
        Task<ItemModelAI?> ImageGenerateContent(string base64Image, string mimeType);
        Task<List<float>?> EmbeddingText(string textEmbeeding);
    }
}
