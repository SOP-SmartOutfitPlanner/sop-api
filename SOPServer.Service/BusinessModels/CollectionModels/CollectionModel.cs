namespace SOPServer.Service.BusinessModels.CollectionModels
{
    public class CollectionModel
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string? ThumbnailURL { get; set; }
        public string UserDisplayName { get; set; }
        public string? AvtUrl { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }
        public int OutfitCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int SavedCount { get; set; }
        public bool IsPublished { get; set; }
        public bool IsFollowing { get; set; }
        public bool IsSaved { get; set; }
        public bool IsLiked { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
