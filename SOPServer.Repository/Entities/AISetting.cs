using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    public class AISetting : BaseEntity
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public AISettingType Type { get; set; }
    }
}
