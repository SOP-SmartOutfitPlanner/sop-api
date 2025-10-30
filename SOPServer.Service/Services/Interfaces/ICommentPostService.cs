﻿using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.CommentPostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ICommentPostService
    {
        Task<BaseResponseModel> CreateNewComment(CreateCommentPostModel model);
        Task<BaseResponseModel> DeleteCommentPost(int id);
        Task<BaseResponseModel> GetCommentByParentId(PaginationParameter paginationParameter, long id);
    }
}
