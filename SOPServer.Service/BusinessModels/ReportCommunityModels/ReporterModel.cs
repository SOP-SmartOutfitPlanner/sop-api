using SOPServer.Service.BusinessModels.UserModels;

namespace SOPServer.Service.BusinessModels.ReportCommunityModels
{
    /// <summary>
    /// Model for a reporter who reported specific content
    /// </summary>
    public class ReporterModel
    {
        /// <summary>
        /// Unique identifier for the reporter record
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// User ID of the reporter
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Basic information about the reporter
        /// </summary>
        public UserBasicModel? Reporter { get; set; }

        /// <summary>
        /// Reporter's description of the issue
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Date when this user reported the content
        /// </summary>
        public DateTime CreatedDate { get; set; }
    }
}
