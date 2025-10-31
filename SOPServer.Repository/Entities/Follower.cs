using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    public partial class Follower : BaseEntity
    {
        public long FollowerId { get; set; }
        public long FollowingId { get; set; }

        public virtual User FollowerUser { get; set; }
        public virtual User FollowingUser { get; set; }
    }
}
