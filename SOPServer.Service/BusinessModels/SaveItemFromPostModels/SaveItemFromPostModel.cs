using SOPServer.Service.BusinessModels.ItemModels;
using System;

namespace SOPServer.Service.BusinessModels.SaveItemFromPostModels
{
    public class SaveItemFromPostModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ItemId { get; set; }
        public long PostId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? ItemName { get; set; }
        public string? ItemImgUrl { get; set; }
        public string? PostBody { get; set; }
        public long? PostUserId { get; set; }
        public string? PostUserDisplayName { get; set; }
    }

    public class SaveItemFromPostDetailedModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ItemId { get; set; }
        public long PostId { get; set; }
        public DateTime CreatedDate { get; set; }
        public ItemModel Item { get; set; }
        public string? PostBody { get; set; }
        public long? PostUserId { get; set; }
        public string? PostUserDisplayName { get; set; }
    }
}
