using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class StyleRepository : GenericRepository<Style>, IStyleRepository
    {
        private readonly SOPServerContext _context;
        public StyleRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
