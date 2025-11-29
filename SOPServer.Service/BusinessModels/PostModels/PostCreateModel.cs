using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.PostModels
{
    public class PostCreateModel
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        public string Body { get; set; }

        public List<string> Hashtags { get; set; } = new List<string>();

        public List<IFormFile> Images { get; set; } = new List<IFormFile>();

        public List<long>? ItemIds { get; set; }

        public long? OutfitId { get; set; }
    }
}
