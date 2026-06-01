using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AITourismPlanner.Services
{
    public interface IWeatherService
    {
        Task<WeatherData> GetCurrentWeatherAsync(string city);
        Task<WeatherForecast> GetWeatherForecastAsync(string city, int days = 5);
    }

    public class WeatherData
    {
        public string City { get; set; }
        public string Country { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public string Condition { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public int Pressure { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class WeatherForecast
    {
        public string City { get; set; }

        public List<DailyForecast> DailyForecasts { get; set; } = new List<DailyForecast>();
    }


    public class DailyForecast
    {
        public DateTime Date { get; set; }
        public double TempMax { get; set; }
        public double TempMin { get; set; }
        public string Condition { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
    }
}