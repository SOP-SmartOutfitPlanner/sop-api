namespace SOPServer.Service.BusinessModels.PostModels
{
    public class PostItemDetailModel
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Brand { get; set; }
        public string ImgUrl { get; set; } = string.Empty;
        public string? AiDescription { get; set; }
        public bool IsDeleted { get; set; }
    }
}
