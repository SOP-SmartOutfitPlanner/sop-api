#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class LikePost : BaseEntity
{
    public long PostId { get; set; }
    public long UserId { get; set; }
    
    public virtual Post Post { get; set; }
    public virtual User User { get; set; }
}
