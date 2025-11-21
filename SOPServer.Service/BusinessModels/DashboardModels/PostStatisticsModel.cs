using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.DashboardModels
{
    /// <summary>
    /// Post statistics dashboard model
    /// </summary>
    public class PostStatisticsModel
    {
        public int TotalPosts { get; set; }
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }
        public int TotalFollowers { get; set; }
        public int FollowersThisMonth { get; set; }
        public List<MonthlyPostStatisticsModel> MonthlyStats { get; set; } = new List<MonthlyPostStatisticsModel>();
        public List<TopPostModel> TopPosts { get; set; } = new List<TopPostModel>();
    }

    /// <summary>
    /// Monthly post statistics breakdown
    /// </summary>
    public class MonthlyPostStatisticsModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; }
        public int PostsCreated { get; set; }
        public int LikesReceived { get; set; }
        public int CommentsReceived { get; set; }
        public int TotalEngagement { get; set; }
    }

    /// <summary>
    /// Top performing post model
    /// </summary>
    public class TopPostModel
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int TotalEngagement { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Filter model for post dashboard statistics
    /// </summary>
    public class PostDashboardFilterModel
    {
        /// <summary>
        /// Filter by specific year (default: current year)
        /// </summary>
        /// <example>2024</example>
        [Range(2020, 2100, ErrorMessage = "Year must be between 2020 and 2100")]
        public int? Year { get; set; }

        /// <summary>
        /// Filter by specific month (1-12, optional)
        /// </summary>
        /// <example>12</example>
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        public int? Month { get; set; }

        /// <summary>
        /// Number of top posts to return (default: 5, min: 1, max: 50)
        /// </summary>
        /// <example>10</example>
        [Range(1, 50, ErrorMessage = "Top posts count must be between 1 and 50")]
        public int TopPostsCount { get; set; } = 5;
    }
}
