using Microsoft.EntityFrameworkCore;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;

namespace SOPServer.Repository.Repositories.Implements
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        private readonly SOPServerContext _context;
        public CategoryRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllChildrenCategory()
        {
            return await _context.Categories
                .Where(c => c.ParentId != null && !c.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Category>> GetAllParentCategory()
        {
            return await _context.Categories
                .Where(c => c.ParentId == null && !c.IsDeleted)
                .ToListAsync();
        }
    }
}
