#nullable disable
using SOPServer.Repository.Enums;
using System;

namespace SOPServer.Repository.Entities
{
    public partial class ReportCommunity : BaseEntity
    {
        public long UserId { get; set; }
        public long? PostId { get; set; }
        public long? CommentId { get; set; }
        public ReportType Type { get; set; }
        public ReportAction Action { get; set; } = ReportAction.NONE;
        public ReportStatus Status { get; set; } = ReportStatus.PENDING;
        public string Description { get; set; }
        public long? ResolvedByAdminId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolutionNotes { get; set; }

        public virtual User User { get; set; }
        public virtual Post Post { get; set; }
        public virtual CommentPost CommentPost { get; set; }
        public virtual User ResolvedByAdmin { get; set; }
    }
}
