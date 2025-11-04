using SOPServer.Repository.Enums;
using System;

namespace SOPServer.Service.BusinessModels.StyleModels
{
    public class StyleModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public CreatedBy CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class StyleItemModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
