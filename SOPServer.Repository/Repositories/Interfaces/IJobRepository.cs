using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IJobRepository : IGenericRepository<Job>
    {
        Task<Job?> GetByNameAsync(string name);
        Task<IEnumerable<Job>> SearchByNameAsync(string search);
    }
}
