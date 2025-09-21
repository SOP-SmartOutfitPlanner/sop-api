using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using AutoMapper;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.Services.Interfaces;
using Xunit;

namespace SOPServer.Service.UnitTest.Items
{
    public class CreateItemTest
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IGeminiService> _geminiServiceMock;
        private readonly ItemService _itemService;

        public CreateItemTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _geminiServiceMock = new Mock<IGeminiService>();
            _itemService = new ItemService(_unitOfWorkMock.Object, _mapperMock.Object, _geminiServiceMock.Object);
        }

        [Fact]
        public async Task AddNewItem_WhenCalled_ThrowsNotImplementedException()
        {
            var model = new ItemCreateModel
            {
                UserId = 1,
                Name = "Test Item",
                CategoryId = 1,
                Color = "Red",
                Brand = "Brand"
            };

            await Assert.ThrowsAsync<NotImplementedException>(() => _itemService.AddNewItem(model));
        }
    }
}
