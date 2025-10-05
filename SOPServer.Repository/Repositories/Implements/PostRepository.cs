using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class PostRepository : GenericRepository<Post>, IPostRepository
    {
        public PostRepository(SOPServerContext context) : base(context)
        {
        }
    }
}
