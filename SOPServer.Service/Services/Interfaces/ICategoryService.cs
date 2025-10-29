using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CategoryModels;
using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<BaseResponseModel> GetCategoryPaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetCategoryByIdAsync(long id);
        Task<BaseResponseModel> GetCategoriesByParentIdAsync(long parentId, PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetRootCategoriesPaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> DeleteCategoryByIdAsync(long id);
        Task<BaseResponseModel> UpdateCategoryByIdAsync(CategoryUpdateModel model);
        Task<BaseResponseModel> CreateCategoryAsync(CategoryCreateModel model);
    }
}
