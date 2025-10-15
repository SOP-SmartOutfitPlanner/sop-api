using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.GeminiModels;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class GeminiService : IGeminiService
    {
        private readonly GenerativeModel _generativeModel;

        private readonly HashSet<string> _allowedMime = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };
        private readonly string _promptValidation = @"
You are a fashion image validator. Analyze the given image and return a JSON object that matches the following C# class:

public class ImageValidation
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
}

Rules:

- If the image meets ALL conditions, return:
  {
    ""IsValid"": true,
    ""Message"": ""The image is of good quality""
  }

- If the image does NOT meet the conditions, return:
  {
    ""IsValid"": false,
    ""Message"": ""<Reason why the image is invalid, written in English>""
  }

Conditions for IsValid = true:
  • The primary subject is a single clothing item (shirt, pants, dress, shoes, bag, hat, scarf, sunglasses, jewelry, belt, etc.).
  • The item is clearly visible, centered, and mostly unobstructed.
  • The image is in focus, sharp, and well-lit enough to recognize the item’s details.
  • The background should be reasonably clean and not overly distracting — it can contain some colors or minimal objects, as long as the clothing item remains the main focus.
  • The content must be safe for work (no nudity, no sensitive/explicit material, no violence, no illegal content).

If the image fails, Message must clearly explain why in English, for example:
  • ""No recognizable clothing item detected.""
  • ""The image contains multiple objects or people.""
  • ""The image is blurry or lacks focus.""
  • ""The clothing item is partially obstructed.""
  • ""The image has a watermark or poor lighting.""
  • ""The background is too complex, making it hard to identify the clothing item.""
  • ""The image contains sensitive or inappropriate content.""

Output strictly in JSON format. Do not include explanations, comments, or code blocks.
";


        private readonly string _promptDescription = @"
You are a professional fashion expert. Analyze the clothing item in the provided image and return a valid JSON object that follows the structure below:

{
  ""Color"": ""The main color of the item (e.g., Black, White, Navy Blue...)"",
  ""AiDescription"": ""A detailed yet concise English description of the clothing item (up to 100 words), describing its appearance, style, and possible use occasions."",
  ""WeatherSuitable"": ""The type of weather suitable for wearing this item (e.g., Summer, Cold weather, Rainy, Mild weather...)"",
  ""Condition"": ""The condition of the item (e.g., New, Used, Slightly worn...)"",
  ""Pattern"": ""The visible pattern if any (e.g., Solid, Striped, Plaid, Floral, Logo...)"",
  ""Fabric"": ""The main fabric or material (e.g., Cotton, Silk, Denim, Wool, Leather, Polyester...)""
}

Only return a valid JSON object. Do not include any explanations, comments, or extra text.
";


        public GeminiService(IOptions<GeminiSettings> geminiSettings)
        {
            var googleAi = new GoogleAi(geminiSettings.Value.APIKey);
            _generativeModel = googleAi.CreateGenerativeModel("models/gemini-2.5-flash");
        }

        public async Task<ItemModelAI?> ImageGenerateContent(string base64Image, string mimeType)
        {
            var request = new GenerateContentRequest();
            request.AddInlineData(base64Image, mimeType);
            request.UseJsonMode<ItemModelAI>();
            request.AddText(_promptDescription);

            return await _generativeModel.GenerateObjectAsync<ItemModelAI>(request);
        }

        public async Task<ImageValidation> ImageValidation(string base64Image, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType) || !_allowedMime.Contains(mimeType))
            {
                throw new NotFoundException(MessageConstants.MIMETYPE_NOT_VALID);
            }

            string? rawMessage = string.Empty;

            var request = new GenerateContentRequest();
            request.AddText(_promptValidation.Trim());
            request.AddInlineData(base64Image, mimeType);
            request.UseJsonMode<ImageValidation>();

            return await _generativeModel.GenerateObjectAsync<ImageValidation>(request);
        }
    }

}
