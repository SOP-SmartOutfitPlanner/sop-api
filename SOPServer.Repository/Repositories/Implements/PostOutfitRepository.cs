using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class PostOutfitRepository : GenericRepository<PostOutfit>, IPostOutfitRepository
    {
        public PostOutfitRepository(SOPServerContext context) : base(context)
        {
        }
    }
}
