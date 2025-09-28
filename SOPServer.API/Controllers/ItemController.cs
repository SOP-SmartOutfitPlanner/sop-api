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

        [HttpGet]
        public Task<IActionResult> GetListItem([FromQuery] PaginationParameter paginationParameter)
        {
            return ValidateAndExecute(async () => await _itemService.GetItemPaginationAsync(paginationParameter));
        }

        [HttpGet("user/{userId}")]
        public Task<IActionResult> GetListItem([FromQuery] PaginationParameter paginationParameter, long userId)
        {
            return ValidateAndExecute(async () => await _itemService.GetItemByUserPaginationAsync(paginationParameter, userId));
        }

        [HttpGet("{id}")]
        public Task<IActionResult> GetItemById(long id)
        {
            return ValidateAndExecute(async () => await _itemService.GetItemById(id));
        }

        [HttpPost("summary")]
        public Task<IActionResult> ValidationImage(IFormFile file)
        {
            return ValidateAndExecute(async () => await _itemService.GetSummaryItem(file));
        }

        [HttpPost]
        public Task<IActionResult> CreateNewItem(ItemCreateModel model)
        {
            return ValidateAndExecute(async () => await _itemService.AddNewItem(model));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteItem(long id)
        {
            return ValidateAndExecute(async () => await _itemService.DeleteItemByIdAsync(id));
        }
        
    }
}
