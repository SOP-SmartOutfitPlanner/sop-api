using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.UserModels;

namespace SOPServer.Service.BusinessModels.ReportCommunityModels
{
    /// <summary>
    /// Report community response model
    /// </summary>
    public class ReportCommunityModel
    {
        /// <summary>
        /// Unique identifier for the report
        /// </summary>
        /// <example>1</example>
        public long Id { get; set; }

        /// <summary>
        /// Basic information about the original reporter (first reporter)
        /// </summary>
        public UserBasicModel? OriginalReporter { get; set; }

        /// <summary>
        /// Basic information about the content author
        /// </summary>
        public UserBasicModel? Author { get; set; }

        /// <summary>
        /// ID of the reported post (if applicable)
        /// </summary>
        /// <example>123</example>
        public long? PostId { get; set; }

        /// <summary>
        /// ID of the reported comment (if applicable)
        /// </summary>
        /// <example>null</example>
        public long? CommentId { get; set; }

        /// <summary>
        /// Type of content reported (POST or COMMENT)
        /// </summary>
        /// <example>POST</example>
        public ReportType Type { get; set; }

        /// <summary>
        /// Action taken by moderators (NONE, HIDE, DELETE, WARN)
        /// </summary>
        /// <example>NONE</example>
        public ReportAction Action { get; set; }

        /// <summary>
        /// Current status of the report (PENDING, REVIEWED, RESOLVED, REJECTED)
        /// </summary>
        /// <example>PENDING</example>
        public ReportStatus Status { get; set; }

        /// <summary>
        /// Total number of users who reported this content
        /// </summary>
        /// <example>5</example>
        public int ReporterCount { get; set; }

        /// <summary>
        /// Date when the report was created
        /// </summary>
        /// <example>2024-01-10T14:35:40Z</example>
        public DateTime CreatedDate { get; set; }
    }
}
