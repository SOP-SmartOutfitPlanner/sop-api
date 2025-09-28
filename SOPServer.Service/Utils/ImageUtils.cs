using Microsoft.AspNetCore.Http;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Utils
{
    public class ImageUtils
    {
        private const int MaxBytes = 10 * 1024 * 1024;
        public async static Task<string> ConvertToBase64Async(IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            var bytes = await ReadAllWithLimitAsync(stream);
            return Convert.ToBase64String(bytes);
        }

        private static async Task<byte[]?> ReadAllWithLimitAsync(Stream s)
        {
            using var ms = new MemoryStream();
            var buf = new byte[81920];
            int read;
            while ((read = await s.ReadAsync(buf, 0, buf.Length)) > 0)
            {
                if (ms.Length + read > MaxBytes) return null;
                ms.Write(buf, 0, read);
            }
            return ms.ToArray();
        }

        public static IFormFile Base64ToFormFile(string base64, string fileName)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Base64 string is null or empty.");

            // Lấy contentType từ prefix, ví dụ: "data:image/png;base64"
            var header = base64.Substring(0, base64.IndexOf(','));
            var contentType = header.Split(':')[1].Split(';')[0]; // -> image/png

            // Bỏ phần header "data:image/png;base64,"
            var base64Data = base64[(base64.IndexOf(',') + 1)..];

            byte[] bytes = Convert.FromBase64String(base64Data);
            var stream = new MemoryStream(bytes);

            return new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
    }
}
