using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IUserDeviceRepository : IGenericRepository<UserDevice>
    {
        Task<UserDevice?> GetByTokenDevice(string deviceToken);
        Task<List<UserDevice>> GetUserDeviceByUserId(long userId);
        Task<List<UserDevice>> GetAllWithUser();
    }
}
