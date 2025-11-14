using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/weathers")]
    [ApiController]
    public class WeatherController : BaseController
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet]
        public Task<IActionResult> GetWeatherForecast(string cityName,[FromQuery] int cnt)
        {
            return ValidateAndExecute(async () => await _weatherService.GetWeatherAsync(cityName, cnt));
        }
    }
}
