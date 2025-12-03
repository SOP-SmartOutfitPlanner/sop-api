using System;

namespace SOPServer.Service.BusinessModels.SaveOutfitFromCollectionModels
{
    public class SaveOutfitFromCollectionModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long OutfitId { get; set; }
        public long CollectionId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? OutfitName { get; set; }
        public string? OutfitDescription { get; set; }
        public string? CollectionTitle { get; set; }
        public long? CollectionUserId { get; set; }
        public string? CollectionUserDisplayName { get; set; }
    }
}
