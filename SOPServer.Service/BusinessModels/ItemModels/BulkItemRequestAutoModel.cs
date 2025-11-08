using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class BulkItemRequestAutoModel
    {
        public long UserId { get; set; }
        public List<string> ImageURLs { get; set; }
    }

    public class BulkItemRequestManualModel
    {
        public long UserId { get; set; }
        public List<BulkItemModel> ItemsUpload { get; set; }
    }

    public class BulkItemModel
    {
        public string ImageURLs { get; set; }
        public long CategoryId { get; set; }
    }
}
