using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.PostModels
{
    public class HashtagModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PostModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string Body { get; set; }
        public List<HashtagModel> Hashtags { get; set; } = new List<HashtagModel>();
        public List<string> Images { get; set; } = new List<string>();
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLiked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PostProjection
    {
        public long PostId { get; set; }
        public long UserId { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UserDisplayName { get; set; } = string.Empty;
        public string? AuthorAvatarUrl { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public List<string> Images { get; set; } = new();
        public List<HashtagProjection> Hashtags { get; set; } = new();
        public int HoursSinceCreation { get; set; }
    }

    public class HashtagProjection
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RankedPost
    {
        public PostProjection Post { get; set; } = new();
        public double RankingScore { get; set; }
    }
}
