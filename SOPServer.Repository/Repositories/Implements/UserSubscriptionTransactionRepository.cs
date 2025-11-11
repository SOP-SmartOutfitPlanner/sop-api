using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class UserSubscriptionTransactionRepository : GenericRepository<UserSubscriptionTransaction>, IUserSubscriptionTransactionRepository
    {
        private readonly SOPServerContext _context;
        public UserSubscriptionTransactionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
