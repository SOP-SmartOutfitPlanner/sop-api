#nullable disable
using System;

namespace SOPServer.Repository.Entities
{
    public partial class UserViolation : BaseEntity
    {
        public long UserId { get; set; }
        public string ViolationType { get; set; }
        public DateTime OccurredAt { get; set; }
        public long? ReportId { get; set; }
        public string Notes { get; set; }

        public virtual User User { get; set; }
        public virtual ReportCommunity Report { get; set; }
    }
}
