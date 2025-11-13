using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.FollowerModels
{
    public class FollowerUserModel
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public string Bio { get; set; }
        public bool IsFollowing { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
