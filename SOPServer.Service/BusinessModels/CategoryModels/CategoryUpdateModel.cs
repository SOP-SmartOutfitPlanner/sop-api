namespace SOPServer.Service.BusinessModels.CategoryModels
{
    public class CategoryUpdateModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long? ParentId { get; set; }
    }
}
