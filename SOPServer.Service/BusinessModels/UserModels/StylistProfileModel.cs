namespace SOPServer.Service.BusinessModels.UserModels
{
    public class StylistProfileModel
    {
        public long Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Location { get; set; }
        public string? Bio { get; set; }
        public DateOnly? Dob { get; set; }
        public long? JobId { get; set; }
        public string? JobName { get; set; }
        public int PublishedCollectionsCount { get; set; }
        public int TotalCollectionsLikes { get; set; }
        public int TotalCollectionsSaves { get; set; }
        public bool IsFollowed { get; set; } = false;
    }
}
