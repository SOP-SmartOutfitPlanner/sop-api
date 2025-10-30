using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.OccasionModels;
using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IOccasionService
    {
        Task<BaseResponseModel> GetOccasionPaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetOccasionByIdAsync(long id);
        Task<BaseResponseModel> DeleteOccasionByIdAsync(long id);
        Task<BaseResponseModel> UpdateOccasionByIdAsync(OccasionUpdateModel model);
        Task<BaseResponseModel> CreateOccasionAsync(OccasionCreateModel model);
    }
}
