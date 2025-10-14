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
You are a strict validator. Analyze the given image and return a JSON object that matches the following C# class:

public class ImageValidation
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
}

Rules:

- If the image meets ALL conditions, return:
  {
    ""IsValid"": true,
    ""Message"": ""Hình ảnh đạt chất lượng""
  }

- If the image does NOT meet the conditions, return:
  {
    ""IsValid"": false,
    ""Message"": ""<Lý do ảnh không đạt bằng tiếng Việt>""
  }

Conditions for IsValid = true:
  • The primary subject is a single clothing item (shirt, pants, dress, shoes, bag, hat, scarf, sunglasses, jewelry, belt, etc.).
  • The item is dominant/centered, fully visible, in focus, sharp, and well-lit.
  • The background must NOT be the same color as the item, must not contain too many colors, and must not contain too many objects.
  • The content is safe for work (no nudity, no sensitive/explicit material, no violence, no illegal content).

If the image fails, Message must clearly explain why in Vietnamese, for example:
  • ""Ảnh có nhiều vật thể hoặc người""
  • ""Ảnh bị mờ, thiếu nét""
  • ""Ảnh bị che khuất một phần""
  • ""Ảnh có watermark hoặc nền quá phức tạp""
  • ""Nền trùng màu với trang phục""
  • ""Nền quá nhiều màu sắc hoặc quá nhiều vật thể""
  • ""Ảnh chứa nội dung nhạy cảm""

Output strictly in JSON format. No explanations, no extra text, no code blocks.
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
