using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using Moq.Protected;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Mappers;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.BusinessModels.FirebaseModels;
using SOPServer.Service.BusinessModels.RemBgModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Repository.Entities;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System;
using System.Linq;

namespace SOPServer.Service.UnitTest.Items
{
    public class GetSummaryItemTest
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGeminiService> _geminiMock = new();
        private readonly Mock<IFirebaseStorageService> _firebaseMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
        private readonly IMapper _mapper;

        public GetSummaryItemTest()
        {
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<MapperConfigProfile>());
            _mapper = mapperConfig.CreateMapper();
        }

        private ItemService CreateService() => new ItemService(
            _uowMock.Object,
            _mapper,
            _geminiMock.Object,
            _firebaseMock.Object,
            _httpClientFactoryMock.Object);

        private static IFormFile BuildFormFile(string content = "fake-bytes", string fileName = "test.png", string contentType = "image/png")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var ms = new MemoryStream(bytes);
            return new FormFile(ms, 0, ms.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private void SetupSuccessfulUploadSequence(string firstFullPath = "path/original.png", string secondFullPath = "path/rembg.png")
        {
            _firebaseMock.SetupSequence(x => x.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new BaseResponseModel
                {
                    StatusCode = 200,
                    Message = MessageConstants.UPLOAD_FILE_SUCCESS,
                    Data = new ImageUploadResult
                    {
                        FileName = "original.png",
                        FullPath = firstFullPath,
                        DownloadUrl = "https://storage/original.png"
                    }
                })
                .ReturnsAsync(new BaseResponseModel
                {
                    StatusCode = 200,
                    Message = MessageConstants.UPLOAD_FILE_SUCCESS,
                    Data = new ImageUploadResult
                    {
                        FileName = "removed.png",
                        FullPath = secondFullPath,
                        DownloadUrl = "https://storage/removed.png"
                    }
                });
        }

        private void SetupHttpClient(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var httpClient = new HttpClient(handlerMock.Object);
            httpClient.BaseAddress = new Uri("https://rembg.mock/");
            _httpClientFactoryMock.Setup(f => f.CreateClient("RembgClient")).Returns(httpClient);
        }

        [Fact]
        public async Task GetSummaryItem_FileNull_ThrowsBadRequest()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<BadRequestException>(() => service.GetSummaryItem(null));
        }

        [Fact]
        public async Task GetSummaryItem_FileEmpty_ThrowsBadRequest()
        {
            var emptyFile = BuildFormFile(string.Empty);
            // Force length zero
            var ms = new MemoryStream();
            var file = new FormFile(ms, 0, 0, "file", "empty.png") { ContentType = "image/png" };
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.GetSummaryItem(file));
            Assert.Equal(MessageConstants.IMAGE_IS_NOT_VALID, ex.Message);
        }

        [Fact]
        public async Task GetSummaryItem_ImageValidationFail_ThrowsBadRequest()
        {
            var file = BuildFormFile();
            _geminiMock.Setup(x => x.ImageValidation(It.IsAny<string>(), file.ContentType)).ReturnsAsync(false);
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.GetSummaryItem(file));
            Assert.Equal(MessageConstants.IMAGE_IS_NOT_VALID, ex.Message);
        }

        [Fact]
        public async Task GetSummaryItem_RembgHttpFailure_ThrowsCallRemBgFail()
        {
            var file = BuildFormFile();
            _geminiMock.Setup(x => x.ImageValidation(It.IsAny<string>(), file.ContentType)).ReturnsAsync(true);
            SetupSuccessfulUploadSequence();
            // HTTP failure status
            SetupHttpClient(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

            _firebaseMock.Setup(x => x.DeleteImageAsync(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponseModel { StatusCode = 200, Message = MessageConstants.DELETE_FILE_SUCCESS });

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.GetSummaryItem(file));
            Assert.Equal(MessageConstants.CALL_REM_BACKGROUND_FAIL, ex.Message);
            _firebaseMock.Verify(x => x.DeleteImageAsync("path/original.png"), Times.Once);
        }

        [Fact]
        public async Task GetSummaryItem_RembgStatusFailed_ThrowsRemBgFail()
        {
            var file = BuildFormFile();
            _geminiMock.Setup(x => x.ImageValidation(It.IsAny<string>(), file.ContentType)).ReturnsAsync(true);
            SetupSuccessfulUploadSequence();

            var failedJson = "{\"status\":\"failed\"}"; // status not succeeded
            SetupHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(failedJson, Encoding.UTF8, "application/json")
            });

            _firebaseMock.Setup(x => x.DeleteImageAsync(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponseModel { StatusCode = 200, Message = MessageConstants.DELETE_FILE_SUCCESS });

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.GetSummaryItem(file));
            Assert.Equal(MessageConstants.REM_BACKGROUND_IMAGE_FAIL, ex.Message);
            _firebaseMock.Verify(x => x.DeleteImageAsync("path/original.png"), Times.Once);
        }

        [Fact]
        public async Task GetSummaryItem_Success_ReturnsSummaryModel()
        {
            var file = BuildFormFile();
            _geminiMock.Setup(x => x.ImageValidation(It.IsAny<string>(), file.ContentType)).ReturnsAsync(true);
            _geminiMock.Setup(x => x.ImageGenerateContent(It.IsAny<string>(), file.ContentType))
                .ReturnsAsync(new ItemModelAI
                {
                    Color = "Blue",
                    AiDescription = "Desc",
                    WeatherSuitable = "Summer",
                    Condition = "New",
                    Pattern = "Solid",
                    Fabric = "Cotton"
                });

            SetupSuccessfulUploadSequence();

            var succeededJson = "{\"status\":\"succeeded\",\"output\":\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes("rembg")) + "\"}";
            SetupHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(succeededJson, Encoding.UTF8, "application/json")
            });

            _firebaseMock.Setup(x => x.DeleteImageAsync(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponseModel { StatusCode = 200, Message = MessageConstants.DELETE_FILE_SUCCESS });

            var service = CreateService();
            var result = await service.GetSummaryItem(file);

            Assert.Equal(200, result.StatusCode);
            Assert.Equal(MessageConstants.GET_SUMMARY_IMAGE_SUCCESS, result.Message);
            var summary = Assert.IsType<ItemSummaryModel>(result.Data);
            Assert.Equal("Blue", summary.Color);
            Assert.Equal("https://storage/removed.png", summary.ImageRemBgURL);
            _firebaseMock.Verify(x => x.DeleteImageAsync("path/original.png"), Times.Once);
            _firebaseMock.Verify(x => x.UploadImageAsync(It.IsAny<IFormFile>()), Times.Exactly(2)); // original + removed bg
        }
    }
}
