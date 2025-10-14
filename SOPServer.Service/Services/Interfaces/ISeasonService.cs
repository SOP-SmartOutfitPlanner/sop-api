using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.SeasonModels;
using SOPServer.Service.BusinessModels.ResultModels;

namespace SOPServer.Service.Services.Interfaces
{
    public interface ISeasonService
    {
        Task<BaseResponseModel> GetSeasonPaginationAsync(PaginationParameter paginationParameter);
        Task<BaseResponseModel> GetSeasonByIdAsync(long id);
        Task<BaseResponseModel> DeleteSeasonByIdAsync(long id);
        Task<BaseResponseModel> UpdateSeasonByIdAsync(SeasonUpdateModel model);
        Task<BaseResponseModel> CreateSeasonAsync(SeasonCreateModel model);
    }
}
