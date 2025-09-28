using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class ItemGoalRepository : GenericRepository<ItemGoal>, IItemGoalRepository
    {
        private readonly SOPServerContext _context;
        public ItemGoalRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
