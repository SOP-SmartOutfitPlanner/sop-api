using GenerativeAI;
using GenerativeAI.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SOPServer.Repository.Enums;
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
        private readonly HttpClient _httpClient;
        private const int MaxBytes = 10 * 1024 * 1024;
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

        public GeminiService(IOptions<GeminiSettings> geminiSettings, IHttpClientFactory httpClientFactory)
        {
            var googleAi = new GoogleAi(geminiSettings.Value.APIKey);

            _generativeModel = googleAi.CreateGenerativeModel("models/gemini-1.5-flash");

            _httpClient = httpClientFactory.CreateClient("FileDownloader");
        }

        public async Task<bool> ImageValidation(string? mimeType, string image, ImageType type)
        {
            //check image is url or base64
            if (type == ImageType.URL)
            {
                if (!Uri.TryCreate(image, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new NotFoundException("Image URL is invalid.");
                }

                using var resp = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();

                mimeType = resp.Content.Headers.ContentType?.MediaType;
                if (string.IsNullOrWhiteSpace(mimeType) || !_allowedMime.Contains(mimeType))
                    throw new NotFoundException(MessageConstants.MIMETYPE_NOT_VALID);

                await using var stream = await resp.Content.ReadAsStreamAsync();
                var bytes = await ReadAllWithLimitAsync(stream, MaxBytes);
                if (bytes is null) throw new NotFoundException(MessageConstants.IMAGE_IS_LARGE);

                image = Convert.ToBase64String(bytes);
            }

            //check mime type
            if (string.IsNullOrWhiteSpace(mimeType) || !_allowedMime.Contains(mimeType))
                throw new NotFoundException(MessageConstants.MIMETYPE_NOT_VALID);

            var request = new GenerateContentRequest();
            request.AddText(_promptValidation.Trim());
            request.AddInlineData(image, mimeType);

            var response = await _generativeModel.GenerateContentAsync(request);

            var raw = response.Text ?? string.Empty;
            var cleaned = CleanOneChar(raw);

            return cleaned == "1";
        }

        private async Task<byte[]?> ReadAllWithLimitAsync(Stream s, int limit)
        {
            using var ms = new MemoryStream();
            var buf = new byte[81920];
            int read;
            while ((read = await s.ReadAsync(buf, 0, buf.Length)) > 0)
            {
                if (ms.Length + read > limit) return null;
                ms.Write(buf, 0, read);
            }
            return ms.ToArray();
        }

        private string CleanOneChar(string x)
        {
            // Loại bỏ ```, dấu nháy, whitespace; chỉ giữ ký tự 0/1 đầu tiên
            var span = x.Trim()
                        .Replace("```", "")
                        .Replace("\n", "")
                        .Replace("\r", "")
                        .Replace("\"", "")
                        .Trim();

            // Nếu model trả kiểu "1." hoặc "1)" -> lấy ký tự số đầu
            foreach (var ch in span)
                if (ch == '0' || ch == '1') return ch.ToString();

            return "0"; // mặc định an toàn
        }
    }
}
