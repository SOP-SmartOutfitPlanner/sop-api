using System;
using System.Collections.Generic;

namespace SOPServer.Service.BusinessModels.WeatherModel
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
        public double FeelsLike { get; set; }
        public double Pressure { get; set; }
        public int Humidity { get; set; }
        public string Description { get; set; }
        public WindInfo Wind { get; set; }
        public int CloudCoverage { get; set; }
        public double? Rain { get; set; }
    }

    public class WindInfo
    {
        public WindSpeed Speed { get; set; }
        public WindDirection Direction { get; set; }
    }

    public class WindSpeed
    {
        public double Value { get; set; }
        public string Unit { get; set; }
        public string Name { get; set; }
    }

    public class WindDirection
    {
        public int Value { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
