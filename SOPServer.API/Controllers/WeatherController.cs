using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;

namespace SOPServer.API.Controllers
{
    [Route("api/v1/weathers")]
    [ApiController]
    public class WeatherController : BaseController
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("search-cities")]
        public Task<IActionResult> SearchCities([FromQuery] string cityName, [FromQuery] int limit = 5)
        {
            return ValidateAndExecute(async () => await _weatherService.GetCitiesByName(cityName, limit));
        }

        [HttpGet("by-coordinates")]
        public Task<IActionResult> GetWeatherByCoordinates([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] int? cnt)
        {
            return ValidateAndExecute(async () => await _weatherService.GetWeatherByCoordinates(latitude, longitude, cnt));
        }
        
        [HttpGet("details-by-coordinates")]
        public Task<IActionResult> GetWeatherDetailsByCoordinates([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] DateTime time)
        {
            return ValidateAndExecute(async () => await _weatherService.GetWeatherDetailsByCoordinates(latitude, longitude, time));
        }
    }
}
