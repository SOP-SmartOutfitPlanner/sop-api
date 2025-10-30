using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.BusinessModels.CommentPostModels
{
    public class CreateCommentPostModel
    {
        public long PostId { get; set; }
        public long UserId { get; set; }
        public string Comment { get; set; }
        public long? ParentCommentId { get; set; }
    }
}
