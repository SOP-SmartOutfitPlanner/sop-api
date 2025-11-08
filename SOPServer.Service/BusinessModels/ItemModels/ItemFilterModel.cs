using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOPServer.Repository.Enums;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ItemFilterModel
    {
        public bool? IsAnalyzed { get; set; }
        public long? CategoryId { get; set; }
        public long? SeasonId { get; set; }
        public long? StyleId { get; set; }
        public long? OccasionId { get; set; }
        public SortOrder? SortByDate { get; set; }
    }
}
