using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CollectionModels;
using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ICollectionService
    {
        Task<BaseResponseModel> GetAllCollectionsPaginationAsync(PaginationParameter paginationParameter, long? callerUserId);
        Task<BaseResponseModel> GetCollectionsByUserPaginationAsync(PaginationParameter paginationParameter, long userId, long? callerUserId);
        Task<BaseResponseModel> GetCollectionByIdAsync(long id, long? callerUserId);
        Task<BaseResponseModel> CreateCollectionAsync(long userId, CollectionCreateModel model);
        Task<BaseResponseModel> UpdateCollectionAsync(long id, long userId, CollectionUpdateModel model);
        Task<BaseResponseModel> DeleteCollectionAsync(long id, long userId);
        Task<BaseResponseModel> TogglePublishCollectionAsync(long id, long userId);
    }
}
