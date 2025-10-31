using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.PostModels
{
    public class PostModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string Body { get; set; }
        public List<string> Hashtags { get; set; } = new List<string>();
        public List<string> Images { get; set; } = new List<string>();
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }
}
