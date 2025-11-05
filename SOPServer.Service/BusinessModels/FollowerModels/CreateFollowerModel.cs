using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.FollowerModels
{
    public class CreateFollowerModel
    {
        public long FollowerId { get; set; }
        public long FollowingId { get; set; }
    }
}
