using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.AISettingModels
{
    public class AISettingRequestModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public AISettingType Type { get; set; }
    }
}
