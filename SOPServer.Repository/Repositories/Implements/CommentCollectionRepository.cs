using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class CommentCollectionRepository : GenericRepository<CommentCollection>, ICommentCollectionRepository
    {
        public CommentCollectionRepository(SOPServerContext context) : base(context)
        {
        }
    }
}
