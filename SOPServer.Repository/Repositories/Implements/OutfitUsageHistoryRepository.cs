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
    public class OutfitUsageHistoryRepository : GenericRepository<OutfitUsageHistory>, IOutfitUsageHistoryRepository
    {
        private readonly SOPServerContext _context;

        public OutfitUsageHistoryRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
