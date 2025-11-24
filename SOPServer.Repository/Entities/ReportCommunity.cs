#nullable disable
using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities
{
    public partial class ReportCommunity : BaseEntity
    {
        public long? PostId { get; set; }
        public long? CommentId { get; set; }
        public ReportType Type { get; set; }
        public ReportAction Action { get; set; } = ReportAction.NONE;
        public ReportStatus Status { get; set; } = ReportStatus.PENDING;
        public long? ResolvedByAdminId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolutionNotes { get; set; }

        public virtual Post Post { get; set; }
        public virtual CommentPost CommentPost { get; set; }
        public virtual User ResolvedByAdmin { get; set; }
        public virtual ICollection<ReportReporter> ReportReporters { get; set; } = new List<ReportReporter>();
    }
}
