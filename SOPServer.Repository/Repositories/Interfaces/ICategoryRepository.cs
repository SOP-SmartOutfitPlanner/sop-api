using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;

namespace SOPServer.Repository.Repositories.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<List<Category>> GetAllChildrenCategory();
        Task<List<Category>> GetAllParentCategory();
    }
}
