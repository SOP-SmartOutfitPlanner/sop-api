using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.CategoryModels
{
    public class CategoryCreateModel
    {
        public string Name { get; set; }
        public long? ParentId { get; set; }
    }
}
