using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class BulkItemRequestModel
    {
        public int UserId { get; set; }
        public List<string> ImageURLs { get; set; }
    }
}
