using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.SplitImageModels
{
    public class SplitImageResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<SplitImageItem> Items { get; set; }
 }

    public class SplitImageItem
    {
        public bool Success { get; set; }
  public string Category { get; set; }
        public string Url { get; set; }
        public string Filename { get; set; }
    }
}
