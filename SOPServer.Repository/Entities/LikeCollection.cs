#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class LikeCollection : BaseEntity
{
    public long CollectionId { get; set; }
    public long UserId { get; set; }

    public virtual Collection Collection { get; set; }
    virtual public User User { get; set; }
}
