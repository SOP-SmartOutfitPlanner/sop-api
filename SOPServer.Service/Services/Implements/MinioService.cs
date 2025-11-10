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

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tiff", ".heic" };
            if (!allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                throw new BadRequestException(MessageConstants.IMAGE_EXTENSION_NOT_SUPPORT);
            }

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

        public async Task<BaseResponseModel> BulkUploadImageAsync(List<IFormFile> files)
        {
            if (files == null || !files.Any())
                throw new NotFoundException(MessageConstants.FILE_NOT_FOUND);

            await EnsureBucketAsync();

            // Upload tất cả files song song
            var uploadTasks = files.Select(async file =>
            {
                try
                {
                    // Validate file
                    if (file == null || file.Length == 0)
                    {
                        return new
                        {
                            Success = false,
                            FileName = file?.FileName ?? "Unknown",
                            Result = (ImageUploadResult)null,
                            Error = MessageConstants.FILE_NOT_FOUND
                        };
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant().Trim();
                    if (ext is not (".jpg" or ".jpeg" or ".png"))
                    {
                        return new
                        {
                            Success = false,
                            FileName = file.FileName,
                            Result = (ImageUploadResult)null,
                            Error = MessageConstants.IMAGE_EXTENSION_NOT_SUPPORT
                        };
                    }

                    var objectName = $"{Guid.NewGuid()}{ext}";

                    using var stream = file.OpenReadStream();

                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(_cfg.Bucket)
                        .WithObject(objectName)
                        .WithStreamData(stream)
                        .WithObjectSize(stream.Length)
                        .WithContentType(file.ContentType);

                    await _minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

                    var downloadUrl = $"{_cfg.PublicEndpoint?.TrimEnd('/')}/{_cfg.Bucket}/{objectName}";

                    return new
                    {
                        Success = true,
                        FileName = file.FileName,
                        Result = new ImageUploadResult
                        {
                            FileName = file.FileName,
                            DownloadUrl = downloadUrl
                        },
                        Error = (string)null
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        Success = false,
                        FileName = file?.FileName ?? "Unknown",
                        Result = (ImageUploadResult)null,
                        Error = ex.Message
                    };
                }
            });

            var results = await Task.WhenAll(uploadTasks);

            // Phân loại kết quả
            var bulkResult = new BulkImageUploadResult();

            foreach (var result in results)
            {
                if (result.Success)
                {
                    bulkResult.SuccessfulUploads.Add(result.Result);
                }
                else
                {
                    bulkResult.FailedUploads.Add(new FailedUploadResult
                    {
                        FileName = result.FileName,
                        Reason = result.Error
                    });
                }
            }

            bulkResult.TotalSuccess = bulkResult.SuccessfulUploads.Count;
            bulkResult.TotalFailed = bulkResult.FailedUploads.Count;

            // Xác định status code dựa trên kết quả
            int statusCode = bulkResult.TotalFailed == 0
                ? StatusCodes.Status200OK
                : bulkResult.TotalSuccess == 0
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status207MultiStatus;

            string message = bulkResult.TotalFailed == 0
                ? MessageConstants.UPLOAD_FILE_SUCCESS
                : bulkResult.TotalSuccess == 0
                ? "All files failed to upload"
                : "Some files failed to upload";

            return new BaseResponseModel
            {
                StatusCode = statusCode,
                Message = message,
                Data = bulkResult
            };
        }

        public async Task<string> DeleteImageByURLAsync(string imgURL)
        {
            if (string.IsNullOrWhiteSpace(imgURL))
                throw new NotFoundException(MessageConstants.FILE_NOT_FOUND);

            var fileName = imgURL.Split('/').Last();

            var args = new RemoveObjectArgs()
                .WithBucket(_cfg.Bucket)
                .WithObject(fileName);

            await _minioClient.RemoveObjectAsync(args);

            return fileName;
        }
    }
}
