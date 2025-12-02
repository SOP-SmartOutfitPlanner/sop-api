#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class SaveOutfitFromPost : BaseEntity
{
    public long UserId { get; set; }
    public long OutfitId { get; set; }
    public long PostId { get; set; }

    public virtual User User { get; set; }
    public virtual Outfit Outfit { get; set; }
    public virtual Post Post { get; set; }
}
