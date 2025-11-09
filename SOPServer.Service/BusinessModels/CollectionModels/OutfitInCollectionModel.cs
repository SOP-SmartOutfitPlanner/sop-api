using System;
using System.Collections.Generic;
using SOPServer.Service.BusinessModels.OutfitModels;

namespace SOPServer.Service.BusinessModels.CollectionModels
{
    public class OutfitInCollectionModel
    {
        public long OutfitId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsSaved { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<OutfitItemModel> Items { get; set; } = new List<OutfitItemModel>();
    }
}
