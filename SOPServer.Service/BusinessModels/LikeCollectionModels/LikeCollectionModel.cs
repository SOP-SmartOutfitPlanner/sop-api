namespace SOPServer.Service.BusinessModels.LikeCollectionModels
{
    public class LikeCollectionModel
    {
        public long Id { get; set; }
        public long CollectionId { get; set; }
  public long UserId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
