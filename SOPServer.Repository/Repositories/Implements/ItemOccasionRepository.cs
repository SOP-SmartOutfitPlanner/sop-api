using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class ItemOccasionRepository : GenericRepository<ItemOccasion>, IItemOccasionRepository
    {
        private readonly SOPServerContext _context;
        public ItemOccasionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
