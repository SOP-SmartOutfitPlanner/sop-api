using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.PostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IPostService
    {
        Task<BaseResponseModel> CreatePostAsync(PostCreateModel model);
        Task<BaseResponseModel> DeletePostByIdAsync(long id);
        Task<BaseResponseModel> GetPostByIdAsync(long id);
        
        /// <summary>
        /// Gets personalized newsfeed for user with Facebook-like refresh dynamics.
        /// Uses Redis caching, time-decay ranking, and diversity enforcement.
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters (pageIndex, pageSize)</param>
        /// <param name="userId">User ID requesting the feed</param>
        /// <param name="sessionId">Optional session ID for seen posts tracking</param>
        /// <returns>Paginated newsfeed with ranked posts</returns>
        Task<BaseResponseModel> GetNewsFeedAsync(PaginationParameter paginationParameter, long userId, string? sessionId = null);
        Task<BaseResponseModel> GetPostByUserIdAsync(PaginationParameter paginationParameter, long userId);
    }
}
