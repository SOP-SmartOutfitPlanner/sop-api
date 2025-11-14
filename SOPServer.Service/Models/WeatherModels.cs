using System;
using System.Collections.Generic;

namespace SOPServer.Service.Models
{
    public class WeatherForecastResponse
    {
        public string CityName { get; set; }
        public List<DailyWeather> DailyForecasts { get; set; } = new List<DailyWeather>();
    }

    public class DailyWeather
    {
        public DateTime Date { get; set; }
        public double Temperature { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public string Description { get; set; }
    }
}
