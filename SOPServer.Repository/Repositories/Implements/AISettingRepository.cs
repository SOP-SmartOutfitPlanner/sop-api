using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class AISettingRepository : GenericRepository<AISetting>, IAISettingRepository
    {
        private readonly SOPServerContext _context;
        public AISettingRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<AISetting?> GetByTypeAsync(AISettingType type)
        {
            return await _context.AISettings.FirstOrDefaultAsync(x => x.Type == type);
        }

    }
}
