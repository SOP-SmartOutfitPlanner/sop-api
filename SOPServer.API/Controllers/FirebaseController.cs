using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/firebase")]
    [ApiController]
    public class FirebaseController : BaseController
    {
        private readonly IFirebaseStorageService _firebaseStorageService;

        public FirebaseController(IFirebaseStorageService firebaseStorageService)
        {
            _firebaseStorageService = firebaseStorageService;
        }

        [HttpPost("upload")]
        public Task<IActionResult> UploadImage(IFormFile file)
        {
            return ValidateAndExecute(async () => await _firebaseStorageService.UploadImageAsync(file));
        }
    }
}
