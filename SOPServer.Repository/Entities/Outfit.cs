using SOPServer.Repository.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class Outfit : BaseEntity
{
    public long? UserId { get; set; }
    public bool isFavorite { get; set; }
    public bool isUsed { get; set; }
    public virtual User User { get; set; }
    public virtual ICollection<OutfitItems> OutfitItems { get; set; } = new List<OutfitItems>();
}