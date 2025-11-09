using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/minio")]
    [ApiController]
    public class MinioController : BaseController
    {
        private readonly IMinioService _minioService;

        public MinioController(IMinioService minioService)
        {
            _minioService = minioService;
        }

        [HttpPost("upload")]
        public Task<IActionResult> UploadImage(IFormFile file)
        {
            return ValidateAndExecute(async () => await _minioService.UploadImageAsync(file));
        }

        [HttpPost("bulk-upload")]
        public Task<IActionResult> UploadBulkImage(List<IFormFile> files)
        {
            return ValidateAndExecute(async () => await _minioService.BulkUploadImageAsync(files));
        }
    }
}
