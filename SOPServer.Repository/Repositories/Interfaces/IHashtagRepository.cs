using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IHashtagRepository : IGenericRepository<Hashtag>
    {
        Task<Hashtag?> GetByNameAsync(string name);
    }
}
