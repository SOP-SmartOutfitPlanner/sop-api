using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Enums;
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
You are a strict validator. Analyze the image and return ONLY a single character:

- Return 1 if ALL conditions are met:
  • The primary subject is a single clothing item (e.g., shirt, pants) or a fashion accessory (e.g., belt, bag, hat, shoes, jewelry, scarf, sunglasses).
  • The item is clearly the main subject (dominant/centered), fully visible, in focus, and sharp (not blurry or low-resolution).
  • The item has clean, well-separated edges so the background can be removed easily (simple or unobtrusive background; minimal occlusion/clutter).
  • The content is SFW (no sensitive or explicit material): no nudity, no sexual/erotic content, no fetish/suggestive poses, no violence/gore, no illegal content.

- Return 0 in ALL other cases:
  • Multiple items or people dominate the frame; faces/person is the main subject; heavy occlusion/cropping; busy/cluttered background.
  • The item is not sharp, is blurry, poorly lit, or partially out of frame; heavy watermarking or compression artifacts.
  • Any sensitive/NSFW or explicit content of any kind.

Output must be exactly 1 or 0. No explanations, no punctuation, no code blocks.
";

        private readonly string _promptDescription = @"
Bạn là một chuyên gia thời trang. Hãy phân tích hình ảnh món đồ trong input và trả về một JSON đúng với cấu trúc sau:

{
  ""Color"": ""Màu sắc chủ đạo của món đồ (ví dụ: Đen, Trắng, Xanh navy...)"",
  ""AiDescription"": ""Mô tả ngắn gọn món đồ bằng tiếng Việt, dễ hiểu, tối đa 2 câu."",
  ""WeatherSuitable"": ""Thời tiết phù hợp để mặc món đồ (ví dụ: Mùa hè, Trời lạnh, Mưa, Thời tiết mát mẻ...)"",
  ""Condition"": ""Tình trạng món đồ (ví dụ: Mới, Đã qua sử dụng, Hơi cũ...)"",
  ""Pattern"": ""Họa tiết nếu có (ví dụ: Trơn, Kẻ sọc, Caro, Hoa văn, Logo...)"",
  ""Fabric"": ""Chất liệu chính (ví dụ: Cotton, Lụa, Denim, Len, Da, Polyester...)""
}

Chỉ trả về JSON hợp lệ, không thêm giải thích, không thêm chữ nào khác.
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

        public async Task<bool> ImageValidation(string base64Image, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType) || !_allowedMime.Contains(mimeType))
            {
                throw new NotFoundException(MessageConstants.MIMETYPE_NOT_VALID);
            }

            string? rawMessage = string.Empty;

            var request = new GenerateContentRequest();
            request.AddText(_promptValidation.Trim());
            request.AddInlineData(base64Image, mimeType);

            var response = await _generativeModel.GenerateContentAsync(request);

            rawMessage = response.Text ?? string.Empty;

            var cleaned = CleanOneChar(rawMessage);

            return cleaned == "1";
        }

        private string CleanOneChar(string x)
        {
            var span = x.Trim()
                        .Replace("```", "")
                        .Replace("\n", "")
                        .Replace("\r", "")
                        .Replace("\"", "")
                        .Trim();

            foreach (var ch in span)
                if (ch == '0' || ch == '1') return ch.ToString();

            return "0";
        }
    }

}
