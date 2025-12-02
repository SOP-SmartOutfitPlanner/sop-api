using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveOutfitFromPostModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ISaveOutfitFromPostService
    {
        Task<BaseResponseModel> SaveOutfitAsync(long userId, SaveOutfitFromPostCreateModel model);
        Task<BaseResponseModel> UnsaveOutfitAsync(long userId, long outfitId, long postId);
        Task<BaseResponseModel> GetSavedOutfitsByUserAsync(long userId);
        Task<BaseResponseModel> CheckIfSavedAsync(long userId, long outfitId, long postId);
    }
}
