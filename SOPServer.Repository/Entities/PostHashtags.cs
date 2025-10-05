using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    public partial class PostHashtags : BaseEntity
    {
        public long PostId { get; set; }
        public long HashtagId { get; set; }
        public virtual Post Post { get; set; }
        public virtual Hashtag Hashtag { get; set; }
    }
}
