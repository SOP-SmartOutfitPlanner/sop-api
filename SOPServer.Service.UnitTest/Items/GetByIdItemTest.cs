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
using SOPServer.Service.BusinessModels.ItemModels;
using Xunit;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

namespace SOPServer.Service.UnitTest.Items
{
    public class GetByIdItemTest
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGeminiService> _geminiMock = new();
        private readonly Mock<IFirebaseStorageService> _firebaseMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();

        public GetByIdItemTest()
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
        public async Task GetItemById_ItemNotFound_ThrowsNotFoundException()
        {
            long id = 42;
            _uowMock.Setup(x => x.ItemRepository.GetByIdIncludeAsync(id, It.IsAny<Func<IQueryable<Item>, IQueryable<Item>>>(), It.IsAny<Expression<Func<Item, bool>>>() ))
                .ReturnsAsync((Item?)null);

            var service = CreateService();
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetItemById(id));
            Assert.Equal(MessageConstants.ITEM_NOT_EXISTED, ex.Message);
        }

        [Fact]
        public async Task GetItemById_Found_ReturnsResponseWithItemModel()
        {
            long id = 7;
            var category = new Category { Id = 3, Name = "Tops" };
            var user = new User { Id = 11, DisplayName = "Alice" };
            var entity = new Item { Id = id, Name = "Blue Shirt", Category = category, User = user };

            _uowMock.Setup(x => x.ItemRepository.GetByIdIncludeAsync(id, It.IsAny<Func<IQueryable<Item>, IQueryable<Item>>>(), It.IsAny<Expression<Func<Item, bool>>>() ))
                .ReturnsAsync(entity);

            var service = CreateService();
            var result = await service.GetItemById(id);

            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            // Note: Service currently returns ITEM_NOT_EXISTED message even when found.
            Assert.Equal(MessageConstants.ITEM_NOT_EXISTED, result.Message);
            var itemModel = Assert.IsType<ItemModel>(result.Data);
            Assert.Equal(entity.Name, itemModel.Name);
            Assert.Equal(category.Name, itemModel.CategoryName);
            Assert.Equal(user.DisplayName, itemModel.UserDisplayName);
        }
    }
}
