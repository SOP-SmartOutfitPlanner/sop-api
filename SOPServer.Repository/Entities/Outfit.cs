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
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsSaved { get; set; }
    public OutfitCreatedBy CreatedBy { get; set; }
    public virtual User User { get; set; }
    public virtual ICollection<OutfitItem> OutfitItems { get; set; } = new List<OutfitItem>();
    public virtual ICollection<OutfitUsageHistory> OutfitUsageHistories { get; set; } = new List<OutfitUsageHistory>();
}