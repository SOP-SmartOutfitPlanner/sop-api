#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class SaveItemFromPost : BaseEntity
{
    public long UserId { get; set; }
    public long ItemId { get; set; }
    public long PostId { get; set; }

    public virtual User User { get; set; }
    public virtual Item Item { get; set; }
    public virtual Post Post { get; set; }
}
