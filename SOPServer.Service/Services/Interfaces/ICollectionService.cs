using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CollectionModels;
using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ICollectionService
    {
        Task<BaseResponseModel> GetAllCollectionsPaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetCollectionsByUserPaginationAsync(PaginationParameter paginationParameter, long userId);
        Task<BaseResponseModel> GetCollectionByIdAsync(long id);
        Task<BaseResponseModel> CreateCollectionAsync(long userId, CollectionCreateModel model);
        Task<BaseResponseModel> UpdateCollectionAsync(long id, long userId, CollectionUpdateModel model);
        Task<BaseResponseModel> DeleteCollectionAsync(long id, long userId);
    }
}
