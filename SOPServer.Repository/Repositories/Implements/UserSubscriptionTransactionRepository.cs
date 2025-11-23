using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class UserSubscriptionTransactionRepository : GenericRepository<UserSubscriptionTransaction>, IUserSubscriptionTransactionRepository
    {
        private readonly SOPServerContext _context;
        public UserSubscriptionTransactionRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<UserSubscriptionTransaction>> GetAllOrderPending()
        {
            return await _context.UserSubscriptionTransaction
                .Where(ust => ust.Status == TransactionStatus.PENDING)
                .ToListAsync();
        }
    }
}
