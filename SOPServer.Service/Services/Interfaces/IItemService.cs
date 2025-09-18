using Microsoft.AspNetCore.Http;
using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IItemService
    {
        Task<BaseResponseModel> DeleteItemByIdAsync(long id);
        Task<BaseResponseModel> GetSummaryItem(IFormFile file);
    }
}
