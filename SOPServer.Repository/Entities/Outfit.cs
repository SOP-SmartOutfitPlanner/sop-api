using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Outfit : BaseEntity
{
    public long UserId { get; set; }
    public bool isFavorite { get; set; }
    public OutfitCreatedBy CreatedBy { get; set; }
    public virtual User User { get; set; }
    public virtual ICollection<OutfitItems> OutfitItems { get; set; } = new List<OutfitItems>();
    public virtual ICollection<OutfitUsageHistory> OutfitUsageHistories { get; set; } = new List<OutfitUsageHistory>();
}