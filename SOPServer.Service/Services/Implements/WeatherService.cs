using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Models;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;

namespace SOPServer.Service.Services.Implements
{
    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OpenWeatherMapSettings _openWeatherMapSettings;

        public WeatherService(IHttpClientFactory httpClientFactory, IOptions<OpenWeatherMapSettings> openWeatherMapSettings)
        {
            _httpClientFactory = httpClientFactory;
            _openWeatherMapSettings = openWeatherMapSettings.Value;
        }

        public async Task<BaseResponseModel> GetWeatherAsync(string cityName, int cnt)
        {
            var client = _httpClientFactory.CreateClient("OpenWeatherMap");
            string fullRequestUri = $"data/2.5/forecast/daily?q={cityName}&cnt={cnt}&units=metric&lang=en&appid={_openWeatherMapSettings.APIKey}";

            var response = await client.GetAsync(fullRequestUri);

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            var result = new WeatherForecastResponse
            {
                CityName = (string)json["city"]?["name"] ?? cityName
            };

            var list = json["list"];
            if (list != null)
            {
                foreach (var day in list)
                {
                    long dt = (long)day["dt"];
                    var date = DateTimeOffset
                        .FromUnixTimeSeconds(dt)
                        .ToLocalTime()
                        .Date;

                    double tempDay = (double)day["temp"]["day"];
                    double tempMin = (double)day["temp"]["min"];
                    double tempMax = (double)day["temp"]["max"];
                    string desc = (string)day["weather"][0]["description"];

                    result.DailyForecasts.Add(new DailyWeather
                    {
                        Date = date,
                        Temperature = tempDay,
                        MinTemperature = tempMin,
                        MaxTemperature = tempMax,
                        Description = desc
                    });
                }
            }
            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_WEATHER_INFO_SUCCESS,
                Data = result
            };
        }
    }
}
