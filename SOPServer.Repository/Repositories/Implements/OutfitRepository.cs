using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class OutfitRepository : GenericRepository<Outfit>, IOutfitRepository
    {
        private readonly SOPServerContext _context;

        public OutfitRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
    }
}
