using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class ItemStyleRepository : GenericRepository<ItemStyle>, IItemStyleRepository
    {
        private readonly SOPServerContext _context;
        public ItemStyleRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
