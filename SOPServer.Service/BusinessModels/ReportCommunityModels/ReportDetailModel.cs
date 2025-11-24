using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.UserModels;

namespace SOPServer.Service.BusinessModels.ReportCommunityModels
{
    public class ReportDetailModel
    {
        public long Id { get; set; }
        public ReportType Type { get; set; }
        public ReportStatus Status { get; set; }
        public ReportAction Action { get; set; }
        public DateTime CreatedDate { get; set; }

        public UserBasicModel OriginalReporter { get; set; } = null!;
        public int ReporterCount { get; set; }
        public ReportedContentModel Content { get; set; } = null!;
        public UserBasicModel Author { get; set; } = null!;

        public long? ResolvedByAdminId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNotes { get; set; }
        public string? HiddenStatus { get; set; }

        public int AuthorWarningCount { get; set; }
        public int AuthorSuspensionCount { get; set; }
    }
}
