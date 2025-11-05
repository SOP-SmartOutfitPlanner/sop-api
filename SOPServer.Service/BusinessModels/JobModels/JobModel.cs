using SOPServer.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.JobModels
{
    public class JobModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public CreatedBy CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; } = null;
    }
}
