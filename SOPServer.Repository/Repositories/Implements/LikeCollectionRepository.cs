using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
 public class LikeCollectionRepository : GenericRepository<LikeCollection>, ILikeCollectionRepository
    {
        private readonly SOPServerContext _context;

      public LikeCollectionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<LikeCollection?> GetByUserAndCollection(long userId, long collectionId)
        {
         return await _context.LikeCollections
       .FirstOrDefaultAsync(lc => lc.UserId == userId && lc.CollectionId == collectionId);
        }
    }
}
