using Microsoft.AspNetCore.Http;
using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.ItemModels
{
    public class ItemModelRequest
    {
        public List<int> ItemIds { get; set; }
    }
}
