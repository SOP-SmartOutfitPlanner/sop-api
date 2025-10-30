using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IOutfitService
    {
        Task<BaseResponseModel> GetOutfitByIdAsync(long id, long userId);
        Task<BaseResponseModel> GetAllOutfitPaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetOutfitByUserPaginationAsync(PaginationParameter paginationParameter, long userId, bool? isFavorite, bool? isSaved);
        Task<BaseResponseModel> CreateOutfitAsync(long userId, OutfitCreateModel model);
        Task<BaseResponseModel> UpdateOutfitAsync(long id, long userId, OutfitUpdateModel model);
        Task<BaseResponseModel> DeleteOutfitAsync(long id, long userId);
        Task<BaseResponseModel> ToggleOutfitFavoriteAsync(long id, long userId);
        Task<BaseResponseModel> ToggleOutfitSaveAsync(long id, long userId);
    }
}
