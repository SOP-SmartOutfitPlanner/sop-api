using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.BusinessModels.UserDeviceModels;
using SOPServer.Service.Services.Interfaces;
using System.Threading.Tasks;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/user-devices")]
    [ApiController]
    [Authorize]
    public class UserDeviceController : BaseController
    {
        private readonly IUserDeviceService _userDeviceService;

        public UserDeviceController(IUserDeviceService userDeviceService)
        {
            _userDeviceService = userDeviceService;
        }

        /// <summary>
        /// Register or update device token for push notifications
        /// </summary>
        [HttpPost]
        public Task<IActionResult> AddDeviceToken([FromBody] CreateUserDeviceModel model)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || long.Parse(userIdClaim) != model.UserId)
            {
                return Task.FromResult<IActionResult>(Forbid());
            }

            return ValidateAndExecute(async () => await _userDeviceService.AddDeviceTokenByUserId(model));
        }

        /// <summary>
        /// Remove device token (logout from device)
        /// </summary>
        [HttpDelete("{token}")]
        public Task<IActionResult> DeleteDeviceToken(string token)
        {
            return ValidateAndExecute(async () => await _userDeviceService.DeleteDeviceToken(token));
        }
    }
}
