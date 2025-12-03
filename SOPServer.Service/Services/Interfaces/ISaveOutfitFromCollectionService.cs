using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.SaveOutfitFromCollectionModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ISaveOutfitFromCollectionService
    {
        Task<BaseResponseModel> SaveOutfitAsync(long userId, SaveOutfitFromCollectionCreateModel model);
        Task<BaseResponseModel> UnsaveOutfitAsync(long userId, long outfitId, long collectionId);
        Task<BaseResponseModel> GetSavedOutfitsByUserAsync(long userId, PaginationParameter paginationParameter);
        Task<BaseResponseModel> CheckIfSavedAsync(long userId, long outfitId, long collectionId);
    }
}
