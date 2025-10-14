using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class PostHashtagsRepository : GenericRepository<PostHashtags>, IPostHashtagsRepository
    {
        public PostHashtagsRepository(SOPServerContext context) : base(context)
        {
        }
    }
}
