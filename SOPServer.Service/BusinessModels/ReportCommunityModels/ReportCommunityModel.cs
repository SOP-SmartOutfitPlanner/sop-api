using SOPServer.Repository.Enums;

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
        /// ID of the user who submitted the report
        /// </summary>
        /// <example>1</example>
        public long UserId { get; set; }

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
        /// Description of the report
        /// </summary>
        /// <example>This post contains inappropriate content</example>
        public string? Description { get; set; }

        /// <summary>
        /// Date when the report was created
        /// </summary>
        /// <example>2024-01-10T14:35:40Z</example>
        public DateTime CreatedDate { get; set; }
    }
}
