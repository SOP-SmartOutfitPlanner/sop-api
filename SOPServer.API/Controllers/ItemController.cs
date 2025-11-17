using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.API.Attributes;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Enums;
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
        public Task<IActionResult> GetListItem([FromQuery] PaginationParameter paginationParameter, long userId, [FromQuery] ItemFilterModel filter)
        {
            return ValidateAndExecute(async () => await _itemService.GetItemByUserPaginationAsync(paginationParameter, userId, filter));
        }

        [HttpGet("{id}")]
        public Task<IActionResult> GetItemById(long id)
        {
            return ValidateAndExecute(async () => await _itemService.GetItemById(id));
        }

        //[HttpPost("analysis")]
        //public Task<IActionResult> ValidationImage(IFormFile file)
        //{
        //    return ValidateAndExecute(async () => await _itemService.GetAnalyzeItem(file));
        //}

        [HttpPost]
        [CheckItemLimit]
        public Task<IActionResult> CreateNewItem(ItemCreateModel model)
        {
            return ValidateAndExecute(async () => await _itemService.AddNewItem(model));
        }

        [HttpPost("analysis")]
        public Task<IActionResult> AnalysisItem(ItemModelRequest itemsRequestId)
        {
            return ValidateAndExecute(async () => await _itemService.AnalysisItem(itemsRequestId));
        }

        [HttpPut("{id}")]
        public Task<IActionResult> UpdateItem(long id, [FromBody] ItemCreateModel model)
        {
            return ValidateAndExecute(() => _itemService.UpdateItemAsync(id, model));
        }

        [HttpDelete("{id}")]
        public Task<IActionResult> DeleteItem(long id)
        {
            return ValidateAndExecute(async () => await _itemService.DeleteItemByIdAsync(id));
        }

        [HttpPost("bulk-upload/auto")]
        [CheckItemLimit]
        public Task<IActionResult> CreateBulkUploadAuto(BulkItemRequestAutoModel bulkUploadModel)
        {
            return ValidateAndExecute(async () => await _itemService.BulkCreateItemAuto(bulkUploadModel));
        }

        [HttpPost("bulk-upload/manual")]
        [CheckItemLimit]
        public Task<IActionResult> CreateBulkUploadManual(BulkItemRequestManualModel bulkUploadModel)
        {
            return ValidateAndExecute(async () => await _itemService.BulkCreateItemManual(bulkUploadModel));
        }

        [HttpGet("stats/{userId}")]
        public Task<IActionResult> GetStatsWardobe(long userId)
        {
            return ValidateAndExecute(async () => await _itemService.GetUserStats(userId));
        }

        /// <summary>
        /// Split item image to extract individual clothing pieces using AI
        /// </summary>
        /// <remarks>
        /// **Roles:** USER, STYLIST, ADMIN
        ///
        /// **Note:** Subject to subscription limits based on user's plan (monthly reset)
        /// </remarks>
        [HttpPost("split-item")]
        [CheckSubscriptionLimit(FeatureCode.SplitItem)]
        public Task<IActionResult> SplitItem(IFormFile file)
        {
            return ValidateAndExecute(async () => await _itemService.SplitItem(file));
        }
    }
}
