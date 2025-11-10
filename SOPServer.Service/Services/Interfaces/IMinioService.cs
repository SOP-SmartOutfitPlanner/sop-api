using Microsoft.AspNetCore.Http;
using SOPServer.Service.BusinessModels.ResultModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Interfaces
{
    public interface IMinioService
    {
        Task<BaseResponseModel> UploadImageAsync(IFormFile file);
        Task<BaseResponseModel> DeleteImageAsync(string fullPath);
        Task<BaseResponseModel> BulkUploadImageAsync(List<IFormFile> files);
        Task<string> DeleteImageByURLAsync(string imgURL);
    }
}
