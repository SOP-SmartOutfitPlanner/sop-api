using SOPServer.Repository.Enums;
using System.ComponentModel.DataAnnotations;

namespace SOPServer.Service.BusinessModels.ReportCommunityModels
{
    /// <summary>
    /// Model for creating a report for community content (posts or comments)
    /// </summary>
    public class ReportCommunityCreateModel
    {
        /// <summary>
        /// ID of the user submitting the report
        /// </summary>
        /// <example>1</example>
        [Required(ErrorMessage = "UserId is required")]
        public long UserId { get; set; }

        /// <summary>
        /// ID of the post being reported (required when Type is POST)
        /// </summary>
        /// <example>123</example>
        public long? PostId { get; set; }

        /// <summary>
        /// ID of the comment being reported (required when Type is COMMENT)
        /// </summary>
        /// <example>null</example>
        public long? CommentId { get; set; }

        /// <summary>
        /// Type of content being reported
        /// </summary>
        /// <example>POST</example>
        [Required(ErrorMessage = "Type is required")]
        public ReportType Type { get; set; }

        /// <summary>
        /// Optional description providing details about the report
        /// </summary>
        /// <example>This post contains inappropriate content</example>
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }
}
