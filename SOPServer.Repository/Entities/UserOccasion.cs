using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities;

public partial class UserOccasion : BaseEntity
{
    public long UserId { get; set; }

    public long? OccasionId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime DateOccasion { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public WeatherSnapshot? WeatherSnapshot { get; set; }
    public virtual Occasion Occasion { get; set; }

    public virtual User User { get; set; }

    public virtual ICollection<OutfitUsageHistory> OutfitUsageHistories { get; set; } = new List<OutfitUsageHistory>();
}

