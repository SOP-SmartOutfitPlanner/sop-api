using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveItemFromPostModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ISaveItemFromPostService
    {
        Task<BaseResponseModel> SaveItemAsync(long userId, SaveItemFromPostCreateModel model);
        Task<BaseResponseModel> UnsaveItemAsync(long userId, long itemId, long postId);
        Task<BaseResponseModel> GetSavedItemsByUserAsync(long userId, PaginationParameter paginationParameter, ItemFilterModel filter);
        Task<BaseResponseModel> CheckIfSavedAsync(long userId, long itemId, long postId);
    }
}
