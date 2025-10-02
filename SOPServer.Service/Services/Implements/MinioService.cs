using Firebase.Storage;
using GenerativeAI.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using SOPServer.Service.BusinessModels.FirebaseModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class MinioService : IMinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinioSettings _cfg;
        public MinioService(IOptions<MinioSettings> minioSettings)
        {
            _cfg = minioSettings.Value;

            _minioClient = new MinioClient()
                .WithEndpoint(_cfg.Endpoint)
                .WithCredentials(_cfg.AccessKey, _cfg.SecretKey)
                .WithSSL(true)
                .Build();
        }
        private static string ContentTypeOf(string ext) => ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        private async Task EnsureBucketAsync()
        {
            var beArgs = new BucketExistsArgs()
                    .WithBucket(_cfg.Bucket);
            bool found = await _minioClient.BucketExistsAsync(beArgs).ConfigureAwait(false);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs()
                        .WithBucket(_cfg.Bucket);
                await _minioClient.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            }
        }

        public async Task<BaseResponseModel> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new NotFoundException(MessageConstants.FILE_NOT_FOUND);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant().Trim();
            if (ext is not (".jpg" or ".jpeg" or ".png"))
                throw new BadRequestException(MessageConstants.IMAGE_EXTENSION_NOT_SUPPORT);

            await EnsureBucketAsync();

            // objectName: images/{guid}.ext  (nếu bạn có prefix khác, thay "images")
            var objectName = $"{Guid.NewGuid()}{ext}";

            using var stream = file.OpenReadStream();

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_cfg.Bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

            // Public URL (bucket public / có reverse proxy)
            var downloadUrl = $"{_cfg.PublicEndpoint?.TrimEnd('/')}/{_cfg.Bucket}/{objectName}";

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.UPLOAD_FILE_SUCCESS,
                Data = new ImageUploadResult
                {
                    FileName = Path.GetFileName(objectName),
                    DownloadUrl = downloadUrl
                }
            };
        }

        public async Task<BaseResponseModel> DeleteImageAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new NotFoundException(MessageConstants.FILE_NOT_FOUND);

            var args = new RemoveObjectArgs()
                .WithBucket(_cfg.Bucket)
                .WithObject(fileName);

            await _minioClient.RemoveObjectAsync(args).ConfigureAwait(false);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.DELETE_FILE_SUCCESS
            };
        }
    }
}
