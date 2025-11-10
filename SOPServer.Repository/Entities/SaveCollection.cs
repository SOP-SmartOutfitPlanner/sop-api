#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class SaveCollection : BaseEntity
{
    public long CollectionId { get; set; }
    public long UserId { get; set; }

    public virtual Collection Collection { get; set; }
    public virtual User User { get; set; }
}
