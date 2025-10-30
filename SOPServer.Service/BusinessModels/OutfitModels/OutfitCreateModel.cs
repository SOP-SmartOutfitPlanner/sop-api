using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.OutfitModels
{
    public class OutfitCreateModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<long> ItemIds { get; set; } = new List<long>();
    }

    public class OutfitUpdateModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<long>? ItemIds { get; set; }
    }
}
