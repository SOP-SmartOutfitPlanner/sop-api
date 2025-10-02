using AutoMapper;
using Moq;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Mappers;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using System;

namespace SOPServer.Service.UnitTest.Items
{
    public class GetListItemPaginationTest
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IGeminiService> _geminiMock = new();
        private readonly Mock<IFirebaseStorageService> _firebaseMock = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();

        public GetListItemPaginationTest()
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

        private Pagination<Item> BuildPagination(List<Item> items, int page, int pageSize, int totalCount)
            => new Pagination<Item>(items, totalCount, page, pageSize);

        [Fact]
        public async Task GetItemPaginationAsync_ReturnsPagedResult()
        {
            var paginationParam = new PaginationParameter { PageIndex = 1, PageSize = 10 };
            var items = new List<Item>
            {
                new Item { Id = 1, Name = "Item1", Category = new Category { Id = 10, Name = "Cat" }, User = new User { Id = 1, DisplayName = "John" } },
                new Item { Id = 2, Name = "Item2", Category = new Category { Id = 10, Name = "Cat" }, User = new User { Id = 1, DisplayName = "John" } },
            };
            var pagination = BuildPagination(items, 1, 10, 2);

            _uowMock.Setup(x => x.ItemRepository.ToPaginationIncludeAsync(
                paginationParam,
                It.IsAny<Func<IQueryable<Item>, IIncludableQueryable<Item, object>>>(),
                null,
                It.IsAny<Func<IQueryable<Item>, IOrderedQueryable<Item>>>()))
                .ReturnsAsync(pagination);

            var service = CreateService();
            var response = await service.GetItemPaginationAsync(paginationParam);

            Assert.Equal(200, response.StatusCode);
            Assert.Equal(MessageConstants.GET_LIST_ITEM_SUCCESS, response.Message);
            Assert.NotNull(response.Data);
        }

        [Fact]
        public async Task GetItemByUserPaginationAsync_ReturnsFilteredPagedResult()
        {
            long userId = 5;
            var paginationParam = new PaginationParameter { PageIndex = 1, PageSize = 10 };
            var items = new List<Item>
            {
                new Item { Id = 1, Name = "UserItem1", UserId = userId, Category = new Category { Id = 11, Name = "Outer" }, User = new User { Id = userId, DisplayName = "Alice" } },
                new Item { Id = 2, Name = "UserItem2", UserId = userId, Category = new Category { Id = 11, Name = "Outer" }, User = new User { Id = userId, DisplayName = "Alice" } },
            };
            var pagination = BuildPagination(items, 1, 10, 2);

            _uowMock.Setup(x => x.ItemRepository.ToPaginationIncludeAsync(
                paginationParam,
                It.IsAny<Func<IQueryable<Item>, IIncludableQueryable<Item, object>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Item, bool>>>(),
                It.IsAny<Func<IQueryable<Item>, IOrderedQueryable<Item>>>()))
                .ReturnsAsync(pagination);

            var service = CreateService();
            var response = await service.GetItemByUserPaginationAsync(paginationParam, userId);

            Assert.Equal(200, response.StatusCode);
            Assert.Equal(MessageConstants.GET_LIST_ITEM_SUCCESS, response.Message);
            Assert.NotNull(response.Data);
        }
    }
}
