using System;
using System.Collections.Generic;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.UserModels;

namespace SOPServer.Service.BusinessModels.StyleModels
{
    public class StyleDetailModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<ItemModel> Items { get; set; } = new List<ItemModel>();
    }
}
