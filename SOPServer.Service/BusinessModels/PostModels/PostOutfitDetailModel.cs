namespace SOPServer.Service.BusinessModels.PostModels
{
    public class PostOutfitDetailModel
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public List<PostItemDetailModel> Items { get; set; } = new List<PostItemDetailModel>();
    }
}
