using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.FirebaseModels
{
    public class BulkImageUploadResult
{
      public List<ImageUploadResult> SuccessfulUploads { get; set; } = new List<ImageUploadResult>();
        public List<FailedUploadResult> FailedUploads { get; set; } = new List<FailedUploadResult>();
    public int TotalSuccess { get; set; }
        public int TotalFailed { get; set; }
    }

    public class FailedUploadResult
    {
    public string FileName { get; set; }
        public string Reason { get; set; }
    }
}
