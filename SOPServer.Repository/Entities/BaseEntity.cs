using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    public class BaseEntity
    {
        public long Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; } = null;

        public bool IsDeleted { get; set; } = false;
    }
}
