using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<int> GetUserCountByMonth(int currentMonth, int currentYear);
        Task<User?> GetUserProfileByIdAsync(long userId);
    }
}
