using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.StyleModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IStyleService
    {
        Task<BaseResponseModel> GetStylePaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetStyleByIdAsync(long id);
        Task<BaseResponseModel> CreateStyleAsync(StyleCreateModel model);
        Task<BaseResponseModel> UpdateStyleByIdAsync(StyleUpdateModel model);
        Task<BaseResponseModel> DeleteStyleByIdAsync(long id);
    }
}
