namespace SOPServer.Service.BusinessModels.ReportCommunityModels
{
    public class ReportedContentModel
    {
        public long ContentId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new List<string>();
        public bool IsHidden { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
