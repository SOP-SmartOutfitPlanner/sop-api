namespace SOPServer.Service.BusinessModels.PostModels
{
    public class PostLikerModel
    {
        public long UserId { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsFollowing { get; set; }
    }
}
