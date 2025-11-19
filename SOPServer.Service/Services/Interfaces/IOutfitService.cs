using GenerativeAI.Tools;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.OutfitCalendarModels;
using SOPServer.Service.BusinessModels.OutfitModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using System;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IOutfitService
    {
        Task<BaseResponseModel> GetOutfitByIdAsync(long id, long userId);
        Task<BaseResponseModel> GetAllOutfitPaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetOutfitByUserPaginationAsync(PaginationParameter paginationParameter,
                                                               long userId,
                                                               bool? isFavorite,
                                                               bool? isSaved,
                                                               DateTime? startDate,
                                                               DateTime? endDate);
        Task<BaseResponseModel> CreateOutfitAsync(long userId, OutfitCreateModel model);
        Task<BaseResponseModel> UpdateOutfitAsync(long id, long userId, OutfitUpdateModel model);
        Task<BaseResponseModel> DeleteOutfitAsync(long id, long userId);
        Task<BaseResponseModel> ToggleOutfitFavoriteAsync(long id, long userId);
        Task<BaseResponseModel> ToggleOutfitSaveAsync(long id, long userId);

        // Calendar methods
        Task<BaseResponseModel> GetOutfitCalendarPaginationAsync(
            PaginationParameter paginationParameter,
            long userId,
            CalendarFilterType? filterType = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? year = null,
            int? month = null);
        Task<BaseResponseModel> GetOutfitCalendarByIdAsync(long id, long userId);
        Task<BaseResponseModel> GetOutfitCalendarByUserOccasionIdAsync(long userOccasionId, long userId);
        Task<BaseResponseModel> CreateOutfitCalendarAsync(long userId, OutfitCalendarCreateModel model);
        Task<BaseResponseModel> UpdateOutfitCalendarAsync(long id, long userId, OutfitCalendarUpdateModel model);
        Task<BaseResponseModel> DeleteOutfitCalendarAsync(long id, long userId);
        Task<BaseResponseModel> OutfitSuggestion(long userId, long? occasionId);
    }
}
