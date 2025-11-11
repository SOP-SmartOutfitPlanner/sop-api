using SOPServer.Service.BusinessModels.LikeCollectionModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ILikeCollectionService
    {
        Task<BaseResponseModel> CreateLikeCollection(CreateLikeCollectionModel model);
    }
}
