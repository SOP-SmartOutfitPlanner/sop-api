using Firebase.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Extensions.Msal;
using SOPServer.Service.BusinessModels.FirebaseModels;
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
        private readonly FirebaseStorage _storage;
        public FirebaseStorageService(IOptions<FirebaseStorageSettings> firebaseStorageSettings)
        {
            _firebaseStorageSettings = firebaseStorageSettings.Value;
            _storage = new FirebaseStorage(_firebaseStorageSettings.BucketName);
        }

        public async Task<BaseResponseModel> DeleteImageAsync(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new NotFoundException(MessageConstants.FILE_NOT_FOUND);
            }

            await _storage.Child(fullPath).DeleteAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DELETE_FILE_SUCCESS
            };
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

            string uploadTask = string.Empty;
            // Upload the file to Firebase Storage
            using (var stream = file.OpenReadStream())
            {
                uploadTask = await _storage
                    .Child(_firebaseStorageSettings.Path)
                    .Child(fileName)
                    .PutAsync(stream);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.UPLOAD_FILE_SUCCESS,
                Data = new ImageUploadResult
                {
                    FileName = fileName,
                    DownloadUrl = uploadTask,
                    FullPath = $"{_firebaseStorageSettings.Path}/{fileName}"
                }
            };
        }
    }
}
