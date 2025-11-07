using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class StyleRepository : GenericRepository<Style>, IStyleRepository
    {
        private readonly SOPServerContext _context;
        public StyleRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Style>> getAllStyleSystem()
        {
            return await _context.Styles
                .Where(s => s.CreatedBy == CreatedBy.SYSTEM && !s.IsDeleted)
                .ToListAsync();
        }
    }
}
