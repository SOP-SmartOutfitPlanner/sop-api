using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.RemBgModels
{
    public class RembgRequest
    {
        public RembgInput Input { get; set; }
    }

    public class RembgInput
    {
        public string Image { get; set; }
    }
}
