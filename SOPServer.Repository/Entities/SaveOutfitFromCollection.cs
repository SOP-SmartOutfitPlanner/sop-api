#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class SaveOutfitFromCollection : BaseEntity
{
    public long UserId { get; set; }
    public long OutfitId { get; set; }
    public long CollectionId { get; set; }

    public virtual User User { get; set; }
    public virtual Outfit Outfit { get; set; }
    public virtual Collection Collection { get; set; }
}
