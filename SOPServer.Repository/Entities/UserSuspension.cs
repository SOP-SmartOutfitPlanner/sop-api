#nullable disable
using System;

namespace SOPServer.Repository.Entities
{
    public partial class UserSuspension : BaseEntity
    {
        public long UserId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Reason { get; set; }
        public long? ReportId { get; set; }
        public long CreatedByAdminId { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual User User { get; set; }
        public virtual ReportCommunity Report { get; set; }
        public virtual User CreatedByAdmin { get; set; }
    }
}
