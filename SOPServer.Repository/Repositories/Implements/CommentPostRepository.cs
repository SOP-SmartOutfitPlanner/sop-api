using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class CommentPostRepository : GenericRepository<CommentPost>, ICommentPostRepository
    {
        private readonly SOPServerContext _context;

        public CommentPostRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
