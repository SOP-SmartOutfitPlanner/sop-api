using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Mappers;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using Xunit;
using System.Threading.Tasks;

namespace SOPServer.Service.UnitTest.Items
{
    public class DeleteItemTest
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGeminiService> _geminiMock = new();
        private readonly Mock<IFirebaseStorageService> _firebaseMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
        private readonly IMapper _mapper;

        public DeleteItemTest()
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

        [Fact]
        public async Task DeleteItemByIdAsync_ItemNotFound_ThrowsNotFoundException()
        {
            long id = 123;
            _uowMock.Setup(x => x.ItemRepository.GetByIdAsync(id)).ReturnsAsync((Item?)null);

            var service = CreateService();

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteItemByIdAsync(id));
            Assert.Equal(MessageConstants.ITEM_NOT_EXISTED, ex.Message);
        }

        [Fact]
        public async Task DeleteItemByIdAsync_ItemExists_SoftDeletesAndReturnsSuccess()
        {
            long id = 456;
            var item = new Item { Id = id, Name = "Sample" };
            _uowMock.Setup(x => x.ItemRepository.GetByIdAsync(id)).ReturnsAsync(item);
            _uowMock.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);

            var service = CreateService();

            var result = await service.DeleteItemByIdAsync(id);

            Assert.Equal(200, result.StatusCode);
            Assert.Equal(MessageConstants.DELETE_ITEM_SUCCESS, result.Message);
            _uowMock.Verify(x => x.ItemRepository.SoftDeleteAsync(item), Times.Once);
            _uowMock.Verify(x => x.SaveAsync(), Times.Once);
        }
    }
}
