using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.ResultModels
{
    public class BaseResponseModel
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public object? Data { get; set; }
    }
}
