using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOPServer.Service.BusinessModels.ItemModels;

namespace SOPServer.Service.BusinessModels.SeasonModels
{
    public class SeasonDetailModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public List<ItemModel> Items { get; set; } = new List<ItemModel>();
    }
}
