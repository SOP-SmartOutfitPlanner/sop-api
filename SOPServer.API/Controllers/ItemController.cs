using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Repository.Commons;
using SOPServer.Service.BusinessModels.ItemModels;
using SOPServer.Service.Services.Interfaces;
using System.Diagnostics;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/items")]
    [ApiController]
    public class ItemController : BaseController
    {
        private readonly IItemService _itemService;
        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpPost("summary")]
        public Task<IActionResult> ValidationImage(IFormFile file)
        {
            return ValidateAndExecute(async () => await _itemService.GetSummaryItem(file));
            throw new NotImplementedException();
        }

        [HttpPost]
        public Task<IActionResult> Create([FromBody] ItemCreateModel model)
        {
            return ValidateAndExecute(() => _itemService.AddNewItem(model));
        }

        [HttpPut("{id}")]
        public Task<IActionResult> Update(long id, [FromBody] ItemCreateModel model)
        {
            return ValidateAndExecute(() => _itemService.UpdateItemAsync(id, model));
        }

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(long id)
        {
            return ValidateAndExecute(() => _itemService.GetItemByIdAsync(id));
        }

        [HttpGet("user/{userId}")]
        public Task<IActionResult> ListByUser(long userId, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var pagination = new PaginationParameter { PageIndex = pageIndex, PageSize = pageSize };
            return ValidateAndExecute(() => _itemService.GetItemsAsync(userId, pagination));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteItem(long id)
        {
            return ValidateAndExecute(async () => await _itemService.DeleteItemByIdAsync(id));
        }

        //Sample CODE

        //[Authorize(Roles = "1,3,4")]
        //[HttpGet("order/{orderCode}")]
        //public Task<IActionResult> GetPaymentByOrderCode(int orderCode)
        //{
        //    return ValidateAndExecute(async () => await _paymentService.GetPaymentInfoByOrderCode(orderCode));
        //}
    }
}
