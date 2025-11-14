using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IPostService
    {
        Task<BaseResponseModel> CreatePostAsync(PostCreateModel model);
        Task<BaseResponseModel> UpdatePostAsync(long id, PostUpdateModel model);
        Task<BaseResponseModel> DeletePostByIdAsync(long id);
        Task<BaseResponseModel> GetAllPostsAsync(PaginationParameter paginationParameter, long? callerUserId);
        Task<BaseResponseModel> GetPostByIdAsync(long id, long? requesterId = null);
        Task<BaseResponseModel> GetPostByUserIdAsync(PaginationParameter paginationParameter, long userId, long? callerUserId);
        Task<BaseResponseModel> GetPostsByHashtagIdAsync(PaginationParameter paginationParameter, long hashtagId, long? requesterId = null);
        Task<BaseResponseModel> GetPostsByHashtagNameAsync(PaginationParameter paginationParameter, string hashtagName, long? callerUserId);
        Task<BaseResponseModel> GetTopContributorsAsync(PaginationParameter paginationParameter, long? userId = null);
        Task<BaseResponseModel> GetPostLikersAsync(PaginationParameter paginationParameter, long postId, long? userId = null);
    }
}
