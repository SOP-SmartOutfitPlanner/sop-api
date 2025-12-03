using SOPServer.Service.BusinessModels.OutfitModels;
using System;

namespace SOPServer.Service.BusinessModels.SaveOutfitFromPostModels
{
    public class SaveOutfitFromPostModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long OutfitId { get; set; }
        public long PostId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? OutfitName { get; set; }
        public string? OutfitDescription { get; set; }
        public string? PostBody { get; set; }
        public long? PostUserId { get; set; }
        public string? PostUserDisplayName { get; set; }
    }

    public class SaveOutfitFromPostDetailedModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long OutfitId { get; set; }
        public long PostId { get; set; }
        public DateTime CreatedDate { get; set; }
        public OutfitModel Outfit { get; set; }
        public string? PostBody { get; set; }
        public long? PostUserId { get; set; }
        public string? PostUserDisplayName { get; set; }
    }
}
