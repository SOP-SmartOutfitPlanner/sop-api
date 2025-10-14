using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class SeasonRepository : GenericRepository<Season>, ISeasonRepository
    {
        private readonly SOPServerContext _context;
        public SeasonRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
