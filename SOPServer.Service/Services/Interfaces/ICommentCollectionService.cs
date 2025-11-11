using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CommentCollectionModels;
using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ICommentCollectionService
    {
        Task<BaseResponseModel> CreateCommentCollection(CreateCommentCollectionModel model);
        Task<BaseResponseModel> UpdateCommentCollection(long id, UpdateCommentCollectionModel model);
        Task<BaseResponseModel> DeleteCommentCollection(long id);
        Task<BaseResponseModel> GetCommentsByCollectionId(PaginationParameter paginationParameter, long collectionId);
    }
}
