using System;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ItemWornAtHistoryModel
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemImgUrl { get; set; }
        public DateTime WornAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
