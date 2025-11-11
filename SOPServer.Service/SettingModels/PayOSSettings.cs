using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.SettingModels
{
    public class PayOSSettings
    {
        public required string ClientId { get; set; }
        public required string ApiKey { get; set; }
        public required string ChecksumKey { get; set; }
        public required string CancelUrl { get; set; }
        public required string ReturnUrl { get; set; }
    }
}
