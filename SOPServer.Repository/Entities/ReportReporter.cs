#nullable disable
using System;

namespace SOPServer.Repository.Entities
{
    /// <summary>
    /// Represents a user who reported a specific content
    /// Multiple users can report the same content, creating multiple ReportReporter records
    /// </summary>
    public partial class ReportReporter : BaseEntity
    {
        public long ReportId { get; set; }
        public long UserId { get; set; }
        public string Description { get; set; }

        public virtual ReportCommunity Report { get; set; }
        public virtual User User { get; set; }
    }
}
