using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Moq;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Mappers;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using Xunit;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace SOPServer.Service.UnitTest.Items
{
    public class CreateItemTest
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGeminiService> _geminiMock = new();
        private readonly Mock<IFirebaseStorageService> _firebaseMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();

        public CreateItemTest()
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
        public async Task AddNewItem_UserNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var model = new ItemCreateModel { UserId = 1, CategoryId = 10, Name = "Item A" };
            _uowMock.Setup(x => x.UserRepository.GetByIdAsync(model.UserId)).ReturnsAsync((User?)null);

            var service = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.AddNewItem(model));
            Assert.Equal(MessageConstants.USER_NOT_EXIST, ex.Message);
        }

        [Fact]
        public async Task AddNewItem_CategoryNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var model = new ItemCreateModel { UserId = 1, CategoryId = 10, Name = "Item A" };
            _uowMock.Setup(x => x.UserRepository.GetByIdAsync(model.UserId)).ReturnsAsync(new User { Id = model.UserId });
            _uowMock.Setup(x => x.CategoryRepository.GetByIdAsync(model.CategoryId)).ReturnsAsync((Category?)null);

            var service = CreateService();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.AddNewItem(model));
            Assert.Equal(MessageConstants.CATEGORY_NOT_EXIST, ex.Message);
        }

        [Fact]
        public async Task AddNewItem_Success_ReturnsResponseWithItemModel()
        {
            // Arrange
            var model = new ItemCreateModel { UserId = 1, CategoryId = 10, Name = "Shirt", Color = "Blue" };
            var user = new User { Id = 1, DisplayName = "John" };
            var category = new Category { Id = 10, Name = "Clothes" };
            var addedItem = new Item { Id = 99, UserId = user.Id, CategoryId = category.Id, Name = model.Name, Color = model.Color };

            _uowMock.Setup(x => x.UserRepository.GetByIdAsync(model.UserId)).ReturnsAsync(user);
            _uowMock.Setup(x => x.CategoryRepository.GetByIdAsync(model.CategoryId)).ReturnsAsync(category);
            _uowMock.Setup(x => x.ItemRepository.AddAsync(It.IsAny<Item>()))
                .Callback<Item>(i => i.Id = addedItem.Id)
                .ReturnsAsync((Item i) => i);
            _uowMock.Setup(x => x.Save()).Returns(1);
            _uowMock.Setup(x => x.ItemRepository.GetByIdIncludeAsync(
                addedItem.Id,
                It.IsAny<Func<IQueryable<Item>, IQueryable<Item>>>(),
                It.IsAny<Expression<Func<Item, bool>>>())).ReturnsAsync(new Item { Id = addedItem.Id, Name = addedItem.Name, Category = category, User = user, Color = addedItem.Color });

            var service = CreateService();

            // Act
            var response = await service.AddNewItem(model);

            // Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal(MessageConstants.ITEM_CREATE_SUCCESS, response.Message);
            var itemModel = Assert.IsType<ItemModel>(response.Data);
            Assert.Equal(model.Name, itemModel.Name);
            Assert.Equal(category.Name, itemModel.CategoryName);
            Assert.Equal(user.DisplayName, itemModel.UserDisplayName);

            _uowMock.Verify(x => x.ItemRepository.AddAsync(It.IsAny<Item>()), Times.Once);
            _uowMock.Verify(x => x.Save(), Times.Once);
            _uowMock.Verify(x => x.ItemRepository.GetByIdIncludeAsync(
                addedItem.Id,
                It.IsAny<Func<IQueryable<Item>, IQueryable<Item>>>(),
                It.IsAny<Expression<Func<Item, bool>>>()), Times.Once);
        }
    }
}
