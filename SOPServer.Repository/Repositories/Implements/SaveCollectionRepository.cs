using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class SaveCollectionRepository : GenericRepository<SaveCollection>, ISaveCollectionRepository
    {
        private readonly SOPServerContext _context;

        public SaveCollectionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<SaveCollection?> GetByUserAndCollection(long userId, long collectionId)
        {
            return await _context.SaveCollection
                .FirstOrDefaultAsync(sc => sc.UserId == userId && sc.CollectionId == collectionId);
        }
    }
}
