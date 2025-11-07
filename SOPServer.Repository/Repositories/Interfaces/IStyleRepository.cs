using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IStyleRepository : IGenericRepository<Style>
    {
        Task<List<Style>> getAllStyleSystem();
    }
}
