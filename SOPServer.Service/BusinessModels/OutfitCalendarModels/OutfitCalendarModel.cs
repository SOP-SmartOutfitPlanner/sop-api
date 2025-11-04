using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.UserOccasionModels;
using System;

namespace SOPServer.Service.BusinessModels.OutfitCalendarModels
{
    public class OutfitCalendarModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public long OutfitId { get; set; }
        public string OutfitName { get; set; }
        public long? UserOccasionId { get; set; }
        public string? UserOccasionName { get; set; }
        public DateTime DateUsed { get; set; }
        public OutfitCreatedBy CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class OutfitCalendarDetailedModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserDisplayName { get; set; }
        public long OutfitId { get; set; }
        public OutfitModel Outfit { get; set; }
        public long? UserOccasionId { get; set; }
        public UserOccasionModel? UserOccasion { get; set; }
        public DateTime DateUsed { get; set; }
        public OutfitCreatedBy CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
