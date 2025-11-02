using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class UserOccasionRepository : GenericRepository<UserOccasion>, IUserOccasionRepository
    {
        private readonly SOPServerContext _context;

        public UserOccasionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
