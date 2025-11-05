using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.LikePostModels
{
    public class LikePostModel
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public long UserId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
