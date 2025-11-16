using SOPServer.Service.BusinessModels.ItemModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.QDrantModels
{
    public class QDrantSearchModels : ItemModelAI
    {
        public int Id { get; set; }
        public float Score { get; set; }
    }
}
