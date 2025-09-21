using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.AuthenModels
{
    public class AuthenResultModel
    {
        public string AccessToken { get; set; } = "";

        public string RefreshToken { get; set; } = "";
    }
}
