using System;

namespace SOPServer.Service.BusinessModels.SaveCollectionModels
{
    public class SaveCollectionModel
    {
        public long Id { get; set; }
        public long CollectionId { get; set; }
        public long UserId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
