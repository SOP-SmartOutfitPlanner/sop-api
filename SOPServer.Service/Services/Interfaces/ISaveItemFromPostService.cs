using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveItemFromPostModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ISaveItemFromPostService
    {
        Task<BaseResponseModel> SaveItemAsync(long userId, SaveItemFromPostCreateModel model);
        Task<BaseResponseModel> UnsaveItemAsync(long userId, long itemId, long postId);
        Task<BaseResponseModel> GetSavedItemsByUserAsync(long userId);
        Task<BaseResponseModel> CheckIfSavedAsync(long userId, long itemId, long postId);
    }
}
