using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Service.BusinessModels.AISettingModels;
using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IAISettingService
    {
        Task<BaseResponseModel> GetAllAsync();
        Task<BaseResponseModel> GetByIdAsync(long id);
        Task<BaseResponseModel> GetByTypeAsync(AISettingType type);
        Task<BaseResponseModel> CreateAsync(AISettingRequestModel model);
        Task<BaseResponseModel> UpdateAsync(long id, AISettingRequestModel model);
        Task<BaseResponseModel> DeleteAsync(long id);
    }
}
