using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.CollectionModels
{
    public class CollectionDetailedModel
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public List<CollectionOutfitModel> Outfits { get; set; } = new List<CollectionOutfitModel>();
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
