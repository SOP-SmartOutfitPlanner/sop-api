using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.WeatherModel;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.SettingModels;
using SOPServer.Service.Utils;
using System.Globalization;
using System.Net;

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

        public async Task<BaseResponseModel> GetCitiesByName(string cityName, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                throw new ArgumentException("City name is required.", nameof(cityName));
            }

            cityName = cityName.Trim();
            limit = Math.Clamp(limit, 1, 10); // Giới hạn từ 1 đến 10 kết quả

            var client = _httpClientFactory.CreateClient("OpenWeatherMap");

            string cityQuery = StringUtils.ConvertToUnSign(cityName);
            string geoUri =
                $"geo/1.0/direct?q={Uri.EscapeDataString(cityQuery)}&limit={limit}&appid={_openWeatherMapSettings.APIKey}";

            using (var geoResponse = await client.GetAsync(geoUri))
            {
                if (!geoResponse.IsSuccessStatusCode)
                {
                    string errorMessage = await BuildOpenWeatherErrorMessageAsync(
                        geoResponse,
                        "Unable to search cities from OpenWeatherMap."
                    );

                    throw new BaseErrorResponseException(errorMessage, (int)geoResponse.StatusCode);
                }

                var geoContent = await geoResponse.Content.ReadAsStringAsync();
                var geoJson = JArray.Parse(geoContent);

                var result = new CitySearchResponse();

                foreach (var city in geoJson)
                {
                    string cityNameEn = (string?)city["name"] ?? "";
                    string? cityNameVi = (string?)city["local_names"]?["vi"];
                    double lat = (double)city["lat"];
                    double lon = (double)city["lon"];
                    string? country = (string?)city["country"];
                    string? state = (string?)city["state"];

                    result.Cities.Add(new CityLocationModel
                    {
                        Name = cityNameEn,
                        LocalName = cityNameVi,
                        Latitude = lat,
                        Longitude = lon,
                        Country = country,
                        State = state
                    });
                }

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.GET_CITIES_SUCCESS,
                    Data = result
                };
            }
        }

        public async Task<BaseResponseModel> GetWeatherByCoordinates(double latitude, double longitude, int? cnt)
        {
            int days = Math.Clamp(cnt ?? 16, 1, 16);

            var client = _httpClientFactory.CreateClient("OpenWeatherMap");

            string latStr = latitude.ToString(CultureInfo.InvariantCulture);
            string lonStr = longitude.ToString(CultureInfo.InvariantCulture);

            // Gọi API để lấy tên thành phố từ tọa độ
            string reverseGeoUri =
                $"geo/1.0/reverse?lat={latStr}&lon={lonStr}&limit=1&appid={_openWeatherMapSettings.APIKey}";

            string cityDisplayName = "Unknown Location";

            using (var reverseGeoResponse = await client.GetAsync(reverseGeoUri))
            {
                if (reverseGeoResponse.IsSuccessStatusCode)
                {
                    var reverseGeoContent = await reverseGeoResponse.Content.ReadAsStringAsync();
                    var reverseGeoJson = JArray.Parse(reverseGeoContent);

                    if (reverseGeoJson.Any())
                    {
                        var locationInfo = reverseGeoJson[0];

                        string? englishName = (string?)locationInfo["local_names"]?["en"];
                        string? defaultName = (string?)locationInfo["name"];
                        string? stateName = (string?)locationInfo["state"];
                        string? countryCode = (string?)locationInfo["country"]; // <- quốc gia (VN, US...)

                        // Ưu tiên city English → nếu không lấy name → nếu không có lấy state
                        string city = englishName
                                      ?? defaultName
                                      ?? stateName
                                      ?? "Unknown";

                        // Build display name: City, State, Country
                        var parts = new List<string>();

                        if (!string.IsNullOrWhiteSpace(city))
                            parts.Add(city);

                        if (!string.IsNullOrWhiteSpace(stateName))
                            parts.Add(stateName);

                        if (!string.IsNullOrWhiteSpace(countryCode))
                            parts.Add(countryCode);

                        cityDisplayName = string.Join(", ", parts);
                    }
                }
            }

            // Gọi forecast/daily bằng lat/lon
            string forecastUri =
                $"data/2.5/forecast/daily?lat={latStr}&lon={lonStr}&cnt={days}&units=metric&lang=en&appid={_openWeatherMapSettings.APIKey}";

            using (var response = await client.GetAsync(forecastUri))
            {
                if (!response.IsSuccessStatusCode)
                {
                    string parsedMessage = await BuildOpenWeatherErrorMessageAsync(
                        response,
                        $"OpenWeatherMap request failed ({(int)response.StatusCode})."
                    );

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new NotFoundException($"Weather forecast for coordinates ({latitude}, {longitude}) not found.");
                    }

                    throw new BaseErrorResponseException(parsedMessage, (int)response.StatusCode);
                }

                var content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                var result = new WeatherForecastResponse
                {
                    CityName = cityDisplayName
                };

                var list = json["list"] as JArray;
                if (list != null)
                {
                    foreach (var day in list)
                    {
                        var dtToken = day["dt"];
                        var tempToken = day["temp"];
                        var weatherArray = day["weather"] as JArray;

                        if (dtToken == null || tempToken == null || weatherArray == null || !weatherArray.Any())
                        {
                            continue;
                        }

                        long dt = (long)dtToken;
                        var date = DateTimeOffset
                            .FromUnixTimeSeconds(dt)
                            .ToLocalTime()
                            .Date;

                        // Temperature data
                        double tempDay = (double)tempToken["day"];
                        double tempMin = (double)tempToken["min"];
                        double tempMax = (double)tempToken["max"];
                        
                        // Feels like temperature
                        double feelsLikeDay = day["feels_like"]?["day"]?.Value<double>() ?? tempDay;
                        
                        // Pressure and humidity
                        double pressure = day["pressure"]?.Value<double>() ?? 0;
                        int humidity = day["humidity"]?.Value<int>() ?? 0;
                        
                        // Weather description
                        string desc = (string)weatherArray[0]["description"];
                        
                        // Wind data
                        var windToken = day["wind"];
                        double windSpeed = windToken?["speed"]?.Value<double>() ?? 0;
                        int windDeg = windToken?["deg"]?.Value<int>() ?? 0;
                        
                        var windInfo = new WindInfo
                        {
                            Speed = new WindSpeed
                            {
                                Value = windSpeed,
                                Unit = "m/s",
                                Name = GetWindSpeedName(windSpeed)
                            },
                            Direction = new WindDirection
                            {
                                Value = windDeg,
                                Code = GetWindDirectionCode(windDeg),
                                Name = GetWindDirectionName(windDeg)
                            }
                        };
                        
                        // Cloud coverage
                        int cloudCoverage = day["clouds"]?.Value<int>() ?? 0;
                        
                        // Rain (precipitation in mm/h)
                        double? rain = day["rain"]?.Value<double?>();

                        result.DailyForecasts.Add(new DailyWeather
                        {
                            Date = date,
                            Temperature = tempDay,
                            MinTemperature = tempMin,
                            MaxTemperature = tempMax,
                            FeelsLike = feelsLikeDay,
                            Pressure = pressure,
                            Humidity = humidity,
                            Description = desc,
                            Wind = windInfo,
                            CloudCoverage = cloudCoverage,
                            Rain = rain
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

        /// <summary>
        /// Parse error response từ OpenWeatherMap thành message thân thiện.
        /// </summary>
        private static async Task<string> BuildOpenWeatherErrorMessageAsync(
            HttpResponseMessage response,
            string defaultMessage)
        {
            string fallback = defaultMessage ?? $"OpenWeatherMap request failed ({(int)response.StatusCode}).";

            string errorContent;
            try
            {
                errorContent = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return fallback;
            }

            if (string.IsNullOrWhiteSpace(errorContent))
            {
                return fallback;
            }

            try
            {
                var errJson = JObject.Parse(errorContent);
                var msg = (string?)errJson["message"];
                var cod = (string?)errJson["cod"];

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    return msg!;
                }

                if (!string.IsNullOrWhiteSpace(cod))
                {
                    return $"OpenWeatherMap error ({cod}).";
                }
            }
            catch
            {
                // ignore parse errors
            }

            return fallback;
        }

        private static string GetWindDirectionCode(int degree)
        {
            var directions = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            int index = (int)Math.Round(((degree % 360) / 22.5));
            if (index >= directions.Length) index = 0;
            return directions[index];
        }

        private static string GetWindDirectionName(int degree)
        {
            var directions = new[] 
            { 
                "North", "North-Northeast", "Northeast", "East-Northeast", 
                "East", "East-Southeast", "Southeast", "South-Southeast",
                "South", "South-Southwest", "Southwest", "West-Southwest",
                "West", "West-Northwest", "Northwest", "North-Northwest"
            };
            int index = (int)Math.Round(((degree % 360) / 22.5));
            if (index >= directions.Length) index = 0;
            return directions[index];
        }

        private static string GetWindSpeedName(double speed)
        {
            if (speed < 0.3) return "Calm";
            if (speed < 1.6) return "Light air";
            if (speed < 3.4) return "Light breeze";
            if (speed < 5.5) return "Gentle breeze";
            if (speed < 8.0) return "Moderate breeze";
            if (speed < 10.8) return "Fresh breeze";
            if (speed < 13.9) return "Strong breeze";
            if (speed < 17.2) return "High wind";
            if (speed < 20.8) return "Gale";
            if (speed < 24.5) return "Strong gale";
            if (speed < 28.5) return "Storm";
            if (speed < 32.7) return "Violent storm";
            return "Hurricane";
        }
    }
}
