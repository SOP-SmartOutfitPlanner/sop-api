using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.PostModels
{
    public class PostUpdateModel
    {
        public string? Body { get; set; }
        
        public List<string>? Hashtags { get; set; }

        public List<IFormFile>? Images { get; set; }
    }
}
