using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using System;

namespace SOPServer.Service.BusinessModels.UserOccasionModels
{
    public class UserOccasionModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public long? OccasionId { get; set; }
        public string? OccasionName { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime DateOccasion { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? WeatherSnapshot { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class UserOccasionDetailedModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public long? OccasionId { get; set; }
        public OccasionItemModel? Occasion { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime DateOccasion { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? WeatherSnapshot { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public OutfitModel? PlannedOutfit { get; set; }
        public bool HasOutfitPlanned { get; set; }
    }
}
