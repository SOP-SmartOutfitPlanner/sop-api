using Microsoft.EntityFrameworkCore;
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
    public class ItemRepository : GenericRepository<Item>, IItemRepository
    {
        private readonly SOPServerContext _context;
        public ItemRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByNameAsync(string name, long userId, long? excludeId = null)
        {
            var query = _context.Items.Where(x => !x.IsDeleted && x.UserId == userId && x.Name == name);

            if (excludeId.HasValue)
                query = query.Where(x => x.Id != excludeId.Value);

            return await query.AnyAsync();
        }

    }
}
