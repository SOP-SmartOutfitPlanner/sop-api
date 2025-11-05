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
        Task<BaseResponseModel> GetAllPostsAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetPostByIdAsync(long id);
    }
}
