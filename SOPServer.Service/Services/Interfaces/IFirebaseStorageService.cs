using Microsoft.AspNetCore.Http;
using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IFirebaseStorageService
    {
        Task<BaseResponseModel> UploadImageAsync(IFormFile file);
    }
}
