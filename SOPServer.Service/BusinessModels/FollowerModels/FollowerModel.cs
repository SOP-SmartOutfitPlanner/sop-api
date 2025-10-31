using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.FollowerModels
{
    public class FollowerModel
    {
        public long Id { get; set; }
        public long FollowerId { get; set; }
        public long FollowingId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
