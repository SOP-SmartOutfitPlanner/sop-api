using SOPServer.Service.BusinessModels.LikePostModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ILikePostService
    {
        Task<BaseResponseModel> CreateLikePost(CreateLikePostModel model);
    }
}
