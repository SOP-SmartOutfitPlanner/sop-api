namespace SOPServer.Service.BusinessModels.CommentCollectionModels
{
    public class CreateCommentCollectionModel
    {
        public long CollectionId { get; set; }
        public long UserId { get; set; }
        public string Comment { get; set; }
    }
}
