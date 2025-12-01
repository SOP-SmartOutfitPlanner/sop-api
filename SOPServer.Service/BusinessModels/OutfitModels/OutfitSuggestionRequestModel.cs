using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    public class OutfitSuggestionRequestModel
    {
        public long UserId { get; set; }
        public string? Weather { get; set; }
        public long? OccasionId { get; set; }
        public long? UserOccasionId { get; set; }
        public int TotalOutfit { get; set; }
    }
}
