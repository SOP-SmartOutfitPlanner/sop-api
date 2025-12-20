using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.SettingModels
{
    public class GeminiSettings
    {
        public string APIKey { get; set; }
        public string ModelID { get; set; } = "models/gemini-2.5-flash";
        public string ServiceAccountKeyPath { get; set; }
    }
}
