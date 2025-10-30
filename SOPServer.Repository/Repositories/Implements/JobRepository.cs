using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class JobRepository : GenericRepository<Job>, IJobRepository
    {
        private readonly SOPServerContext _context;
        public JobRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Job?> GetByNameAsync(string name)
        {
            return await _context.Jobs.FirstOrDefaultAsync(x => x.Name == name);
        }

        public async Task<IEnumerable<Job>> SearchByNameAsync(string search)
        {
            return await _context.Jobs
                .Where(x => x.Name.Contains(search))
                .ToListAsync();
        }
    }
}
