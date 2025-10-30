using SOPServer.Service.BusinessModels.JobModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IJobService
    {
        Task<BaseResponseModel> GetAllAsync(string? search = null);
        Task<BaseResponseModel> GetByIdAsync(long id);
        Task<BaseResponseModel> CreateAsync(JobRequestModel model);
        Task<BaseResponseModel> UpdateAsync(long id, JobRequestModel model);
        Task<BaseResponseModel> DeleteAsync(long id);
    }
}
