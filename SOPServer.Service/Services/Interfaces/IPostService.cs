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
        /// Gets personalized newsfeed for user with simple ranking algorithm.
        /// Posts are ranked by recency (40%) and engagement (60%).
        /// No Redis caching required - uses direct SQL queries with proper indexes.
        /// </summary>
        /// <param name="paginationParameter">Pagination parameters (pageIndex, pageSize)</param>
        /// <param name="userId">User ID requesting the feed</param>
        /// <param name="sessionId">Not used - kept for backward compatibility</param>
        /// <returns>Paginated newsfeed with ranked posts</returns>
        Task<BaseResponseModel> GetNewsFeedAsync(PaginationParameter paginationParameter, long userId, string? sessionId = null);
        Task<BaseResponseModel> GetPostByUserIdAsync(PaginationParameter paginationParameter, long userId);
    }
}
