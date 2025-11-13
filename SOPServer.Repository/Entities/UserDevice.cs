using System;

namespace SOPServer.Repository.Entities
{
    public partial class UserDevice : BaseEntity
    {
        public long UserId { get; set; }
        public string DeviceToken { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;
    }
}
