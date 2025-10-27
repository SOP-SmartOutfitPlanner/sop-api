using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class OccasionRepository : GenericRepository<Occasion>, IOccasionRepository
    {
        private readonly SOPServerContext _context;

        public OccasionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
