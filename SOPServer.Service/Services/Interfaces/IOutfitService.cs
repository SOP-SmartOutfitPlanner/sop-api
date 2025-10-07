using SOPServer.Service.BusinessModels.ResultModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IOutfitService
    {
        Task<BaseResponseModel> ToggleOutfitFavoriteAsync(long id);
        Task<BaseResponseModel> MarkOutfitAsUsedAsync(long id);
        Task<BaseResponseModel> GetOutfitByIdAsync(long id);
    }
}
