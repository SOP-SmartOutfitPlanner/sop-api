using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        //Sample CODE

        //[Authorize(Roles = "1,3,4")]
        //[HttpGet("order/{orderCode}")]
        //public Task<IActionResult> GetPaymentByOrderCode(int orderCode)
        //{
        //    return ValidateAndExecute(async () => await _paymentService.GetPaymentInfoByOrderCode(orderCode));
        //}
    }
}
