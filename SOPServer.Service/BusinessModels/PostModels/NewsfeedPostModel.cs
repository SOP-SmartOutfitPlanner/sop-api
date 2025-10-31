using System;

namespace SOPServer.Service.BusinessModels.PostModels
{
    /// <summary>
    /// Model for newsfeed post with enriched engagement data.
    /// Extends base PostModel with additional metadata for feed display.
    /// </summary>
    public class NewsfeedPostModel : PostModel
    {
        /// <summary>
        /// Number of likes on this post.
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// Number of comments on this post.
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// Whether the current user has liked this post.
        /// </summary>
        public bool IsLikedByUser { get; set; }

        /// <summary>
        /// Author's avatar URL.
        /// </summary>
        public string? AuthorAvatarUrl { get; set; }

        /// <summary>
        /// Computed ranking score (for debugging/analytics).
        /// Not displayed to end users.
        /// </summary>
        public double? RankingScore { get; set; }
    }
}
