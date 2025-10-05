using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class HashtagRepository : GenericRepository<Hashtag>, IHashtagRepository
    {
        private readonly SOPServerContext _context;

        public HashtagRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Hashtag?> GetByNameAsync(string name)
        {
            return await _context.Set<Hashtag>()
                .Where(h => !h.IsDeleted)
                .FirstOrDefaultAsync(h => h.Name.ToLower() == name.ToLower());
        }
    }
}
