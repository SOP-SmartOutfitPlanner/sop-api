using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.SettingModels
{
    public class MinioSettings
    {
        public string Endpoint { get; set; }
        public string Bucket { get; set; }
        public string PublicEndpoint { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
    }
}
