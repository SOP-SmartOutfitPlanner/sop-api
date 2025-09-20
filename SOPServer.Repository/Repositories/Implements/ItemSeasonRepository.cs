using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class ItemSeasonRepository : GenericRepository<ItemSeason>, IItemSeasonRepository
    {
        private readonly SOPServerContext _context;
        public ItemSeasonRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
