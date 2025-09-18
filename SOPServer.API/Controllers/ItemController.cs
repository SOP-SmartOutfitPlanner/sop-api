using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IGeminiService _geminiService;
        public ItemController(IItemService itemService, IGeminiService geminiService)
        {
            _itemService = itemService;
            _geminiService = geminiService;
        }

        //[HttpPost("validation")]
        //public Task<IActionResult> ValidationImage([FromForm] ItemModelRequestAI request)
        //{
        //    //return ValidateAndExecute(async () => await _paymentService.GetPaymentInfoByOrderCode(orderCode));
        //    throw new NotImplementedException();
        //}

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
