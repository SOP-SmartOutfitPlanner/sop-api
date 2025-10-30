using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities;
public partial class OutfitUsageHistory : BaseEntity
{
    public long UserId { get; set; }
    public long OutfitId { get; set; }
    public long? UserOccassionId { get; set; }
    public DateTime DateUsed { get; set; }
    public OutfitCreatedBy CreatedBy { get; set; }
    public virtual User User { get; set; }
    public virtual Outfit Outfit { get; set; }
    public virtual UserOccasion UserOccasion { get; set; }  

}

