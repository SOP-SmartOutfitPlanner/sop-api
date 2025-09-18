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
    public class AIUtils
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
    }
}
