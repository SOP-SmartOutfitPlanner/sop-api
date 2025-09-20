using Firebase.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class FirebaseStorageService : IFirebaseStorageService
    {
        private readonly FirebaseStorageSettings _firebaseStorageSettings;
        public FirebaseStorageService(IOptions<FirebaseStorageSettings> firebaseStorageSettings)
        {
            _firebaseStorageSettings = firebaseStorageSettings.Value;
        }

        public async Task<BaseResponseModel> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new NotFoundException(MessageConstants.FILE_NOT_FOUND);
            }

            // Check file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLower().Trim();
            if (fileExtension != ".jpg" && fileExtension != ".jpeg" && fileExtension != ".png")
            {
                throw new BadRequestException(MessageConstants.IMAGE_EXTENSION_NOT_SUPPORT);
            }

            // Generate a unique file name
            var fileName = $"{Guid.NewGuid()}{fileExtension}";

            // Create Firebase Storage instance
            var storage = new FirebaseStorage(_firebaseStorageSettings.BucketName);

            // Upload the file to Firebase Storage
            using (var stream = file.OpenReadStream())
            {
                var uploadTask = await storage
                    .Child("SOP")
                    .Child(fileName)
                    .PutAsync(stream);

                // Return success with the download URL
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.UPLOAD_FILE_SUCCESS,
                    Data = uploadTask
                };
            }
        }
    }
}
