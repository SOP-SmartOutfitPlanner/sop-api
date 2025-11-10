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
        Task<BaseResponseModel> GetAllPostsAsync(PaginationParameter paginationParameter, long userId);
        Task<BaseResponseModel> GetPostByIdAsync(long id);
        Task<BaseResponseModel> GetPostByUserIdAsync(PaginationParameter paginationParameter, long userId);
        Task<BaseResponseModel> GetPostsByHashtagIdAsync(PaginationParameter paginationParameter, long hashtagId);
        Task<BaseResponseModel> GetTopContributorsAsync(PaginationParameter paginationParameter, long? userId = null);
        Task<BaseResponseModel> GetPostLikersAsync(PaginationParameter paginationParameter, long postId, long? userId = null);
    }
}
