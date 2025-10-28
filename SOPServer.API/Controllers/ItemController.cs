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

        [HttpPost("analysis")]
        public Task<IActionResult> ValidationImage(IFormFile file)
        {
            return ValidateAndExecute(async () => await _itemService.GetAnalyzeItem(file));
        }

        [HttpPost]
        public Task<IActionResult> CreateNewItem(ItemCreateModel model)
        {
            return ValidateAndExecute(async () => await _itemService.AddNewItem(model));
        }

        [HttpPut("{id}")]
        public Task<IActionResult> Update(long id, [FromBody] ItemCreateModel model)
        {
            return ValidateAndExecute(() => _itemService.UpdateItemAsync(id, model));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteItem(long id)
        {
            return ValidateAndExecute(async () => await _itemService.DeleteItemByIdAsync(id));
        }

        /// <summary>
        /// Add occasions to an item
        /// </summary>
        [HttpPost("occasions")]
        public Task<IActionResult> AddOccasionsToItem([FromBody] AddOccasionsToItemModel model)
        {
            return ValidateAndExecute(() => _itemService.AddOccasionsToItemAsync(model));
        }

        /// <summary>
        /// Remove a specific occasion from an item
        /// </summary>
        [HttpDelete("occasions")]
        public Task<IActionResult> RemoveOccasionFromItem([FromBody] RemoveOccasionFromItemModel model)
        {
            return ValidateAndExecute(() => _itemService.RemoveOccasionFromItemAsync(model));
        }

        /// <summary>
        /// Replace all occasions for an item
        /// </summary>
        [HttpPut("occasions")]
        public Task<IActionResult> ReplaceOccasionsForItem([FromBody] ReplaceOccasionsForItemModel model)
        {
            return ValidateAndExecute(() => _itemService.ReplaceOccasionsForItemAsync(model));
        }

        /// <summary>
        /// Add styles to an item
        /// </summary>
        [HttpPost("styles")]
        public Task<IActionResult> AddStylesToItem([FromBody] AddStylesToItemModel model)
        {
            return ValidateAndExecute(() => _itemService.AddStylesToItemAsync(model));
        }

        /// <summary>
        /// Remove a specific style from an item
        /// </summary>
        [HttpDelete("styles")]
        public Task<IActionResult> RemoveStyleFromItem([FromBody] RemoveStyleFromItemModel model)
        {
            return ValidateAndExecute(() => _itemService.RemoveStyleFromItemAsync(model));
        }

        /// <summary>
        /// Replace all styles for an item
        /// </summary>
        [HttpPut("styles")]
        public Task<IActionResult> ReplaceStylesForItem([FromBody] ReplaceStylesForItemModel model)
        {
            return ValidateAndExecute(() => _itemService.ReplaceStylesForItemAsync(model));
        }
    }
}
