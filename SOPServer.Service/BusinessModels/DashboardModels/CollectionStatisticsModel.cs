using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.DashboardModels
{
    /// <summary>
    /// Collection statistics dashboard model
    /// </summary>
    public class CollectionStatisticsModel
    {
        public int TotalCollections { get; set; }
        public int PublishedCollections { get; set; }
        public int UnpublishedCollections { get; set; }
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }
        public int TotalSaves { get; set; }
        public List<MonthlyStatisticsModel> MonthlyStats { get; set; } = new List<MonthlyStatisticsModel>();
        public List<TopCollectionModel> TopCollections { get; set; } = new List<TopCollectionModel>();
    }

    /// <summary>
    /// Monthly statistics breakdown
    /// </summary>
    public class MonthlyStatisticsModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; }
        public int CollectionsCreated { get; set; }
        public int LikesReceived { get; set; }
        public int CommentsReceived { get; set; }
        public int SavesReceived { get; set; }
        public int TotalEngagement { get; set; }
    }

    /// <summary>
    /// Top performing collection model
    /// </summary>
    public class TopCollectionModel
    {
        public long Id { get; set; }
        public string? ThumbnailURL { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public bool IsPublished { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int SaveCount { get; set; }
        public int TotalEngagement { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Filter model for dashboard statistics
    /// </summary>
    public class DashboardFilterModel
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
        /// Number of top collections to return (default: 5, min: 1, max: 50)
        /// </summary>
        /// <example>10</example>
        [Range(1, 50, ErrorMessage = "Top collections count must be between 1 and 50")]
        public int TopCollectionsCount { get; set; } = 5;
    }
}
