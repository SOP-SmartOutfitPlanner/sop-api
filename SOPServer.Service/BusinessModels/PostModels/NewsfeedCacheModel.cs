using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.PostModels
{
    /// <summary>
    /// Model for caching ranked posts in Redis
    /// </summary>
    public class NewsfeedCacheModel
    {
        public List<RankedPost> RankedPosts { get; set; } = new();
   public int TotalCount { get; set; }
      public DateTime CachedAt { get; set; }
    }
}
