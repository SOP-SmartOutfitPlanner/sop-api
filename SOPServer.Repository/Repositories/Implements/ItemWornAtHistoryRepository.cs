using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class ItemWornAtHistoryRepository : GenericRepository<ItemWornAtHistory>, IItemWornAtHistoryRepository
    {
        private readonly SOPServerContext _context;

        public ItemWornAtHistoryRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
