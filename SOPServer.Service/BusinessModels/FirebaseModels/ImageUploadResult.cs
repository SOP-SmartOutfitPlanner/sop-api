using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.FirebaseModels
{
    public class ImageUploadResult
    {
        public string FileName { get; set; }
        public string DownloadUrl { get; set; }
    }
}
