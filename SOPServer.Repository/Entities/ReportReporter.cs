using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    /// <summary>
    /// Represents a user who reported a specific content
    /// Multiple users can report the same content, creating multiple ReportReporter records
    /// </summary>
    public partial class ReportReporter : BaseEntity
    {
        public long ReportCommunityId { get; set; }
        public long UserId { get; set; }
        public string Description { get; set; }

        public virtual ReportCommunity ReportCommunity { get; set; }
        public virtual User User { get; set; }
    }
}
