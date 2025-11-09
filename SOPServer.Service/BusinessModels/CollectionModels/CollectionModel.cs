using System;

namespace SOPServer.Service.BusinessModels.CollectionModels
{
    public class CollectionModel
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string UserDisplayName { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public int OutfitCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
