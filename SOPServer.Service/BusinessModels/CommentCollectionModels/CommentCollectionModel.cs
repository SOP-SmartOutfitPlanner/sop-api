using System;

namespace SOPServer.Service.BusinessModels.CommentCollectionModels
{
    public class CommentCollectionModel
    {
        public long Id { get; set; }
        public long CollectionId { get; set; }
        public long UserId { get; set; }
   public string UserDisplayName { get; set; }
        public string UserAvatarUrl { get; set; }
        public string Comment { get; set; }
public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
