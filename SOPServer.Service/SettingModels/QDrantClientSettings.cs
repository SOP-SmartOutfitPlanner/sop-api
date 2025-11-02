using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.SettingModels
{
    public class QDrantClientSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Collection { get; set; }
        public string SecretKey { get; set; }
        public string Size { get; set; }
    }
}
