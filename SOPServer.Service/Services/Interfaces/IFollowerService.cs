using SOPServer.Service.BusinessModels.FollowerModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IFollowerService
    {
        Task<BaseResponseModel> FollowUser(CreateFollowerModel model);
        Task<BaseResponseModel> UnfollowUser(long id);
        Task<BaseResponseModel> GetFollowerCount(long userId);
        Task<BaseResponseModel> GetFollowingCount(long userId);
        Task<BaseResponseModel> IsFollowing(long followerId, long followingId);
    }
}
