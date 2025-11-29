using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class PostItemRepository : GenericRepository<PostItem>, IPostItemRepository
    {
        public PostItemRepository(SOPServerContext context) : base(context)
        {
        }
    }
}
