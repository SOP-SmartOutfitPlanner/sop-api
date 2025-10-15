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

        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}
