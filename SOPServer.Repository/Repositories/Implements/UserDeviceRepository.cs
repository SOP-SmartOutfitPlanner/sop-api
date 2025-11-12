using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class UserDeviceRepository : GenericRepository<UserDevice>, IUserDeviceRepository
    {
        private readonly SOPServerContext _context;

        public UserDeviceRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<UserDevice?> GetByTokenDevice(string deviceToken)
        {
            return await _context.Set<UserDevice>()
                .Where(ud => ud.DeviceToken == deviceToken && !ud.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<List<UserDevice>> GetUserDeviceByUserId(long userId)
        {
            return await _context.Set<UserDevice>()
                .Where(ud => ud.UserId == userId && !ud.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<UserDevice>> GetAllWithUser()
        {
            return await _context.Set<UserDevice>()
                .Include(ud => ud.User)
                .Where(ud => !ud.IsDeleted && !ud.User.IsDeleted)
                .ToListAsync();
        }
    }
}
