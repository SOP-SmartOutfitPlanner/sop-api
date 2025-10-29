using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.SeasonModels
{
    public class SeasonModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public class SeasonItemModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
