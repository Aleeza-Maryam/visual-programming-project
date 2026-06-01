using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AITourismPlanner.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public WeatherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["WeatherAPI:Key"];
            _baseUrl = configuration["WeatherAPI:BaseUrl"] ?? "https://api.openweathermap.org/data/2.5";
        }

        public async Task<WeatherData> GetCurrentWeatherAsync(string city)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_OPENWEATHER_API_KEY_HERE")
                {
                    return GetMockWeather(city);
                }

                var url = $"{_baseUrl}/weather?q={city},PK&units=metric&appid={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return GetMockWeather(city);
                }

                var json = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(json);

                var weather = new WeatherData
                {
                    City = data.name?.ToString() ?? city,
                    Country = data.sys?.country?.ToString() ?? "PK",
                    Temperature = data.main?.temp != null ? double.Parse(data.main.temp.ToString()) : 0,
                    FeelsLike = data.main?.feels_like != null ? double.Parse(data.main.feels_like.ToString()) : 0,
                    TempMin = data.main?.temp_min != null ? double.Parse(data.main.temp_min.ToString()) : 0,
                    TempMax = data.main?.temp_max != null ? double.Parse(data.main.temp_max.ToString()) : 0,
                    Humidity = data.main?.humidity != null ? int.Parse(data.main.humidity.ToString()) : 0,
                    WindSpeed = data.wind?.speed != null ? double.Parse(data.wind.speed.ToString()) : 0,
                    Pressure = data.main?.pressure != null ? int.Parse(data.main.pressure.ToString()) : 0,
                    LastUpdated = DateTime.Now
                };

                if (data.weather != null && data.weather.Count > 0)
                {
                    weather.Condition = data.weather[0]?.main?.ToString() ?? "Clear";
                    weather.Description = data.weather[0]?.description?.ToString() ?? "Clear sky";
                    weather.Icon = $"https://openweathermap.org/img/w/{data.weather[0]?.icon}.png";
                }

                return weather;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Weather API Error: {ex.Message}");
                return GetMockWeather(city);
            }
        }

        public async Task<WeatherForecast> GetWeatherForecastAsync(string city, int days = 5)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "657011025db3afd05c1605cbdaf7bd54")
                {
                    return GetMockForecast(city, days);
                }

                var url = $"{_baseUrl}/forecast?q={city},PK&units=metric&cnt={days * 8}&appid={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return GetMockForecast(city, days);
                }

                var json = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(json);

                var forecast = new WeatherForecast
                {
                    City = data.city?.name?.ToString() ?? city,
                    DailyForecasts = new System.Collections.Generic.List<DailyForecast>()
                };

                var dailyData = new System.Collections.Generic.Dictionary<string, DailyForecast>();

                if (data.list != null)
                {
                    foreach (var item in data.list)
                    {
                        string dateStr = item.dt_txt?.ToString().Split(' ')[0];
                        DateTime date = DateTime.Parse(dateStr);
                        double temp = item.main?.temp != null ? double.Parse(item.main.temp.ToString()) : 0;
                        string condition = item.weather != null && item.weather.Count > 0 ? item.weather[0]?.main?.ToString() : "Clear";
                        string description = item.weather != null && item.weather.Count > 0 ? item.weather[0]?.description?.ToString() : "Clear sky";
                        string icon = item.weather != null && item.weather.Count > 0 ? item.weather[0]?.icon?.ToString() : "01d";
                        int humidity = item.main?.humidity != null ? int.Parse(item.main.humidity.ToString()) : 0;
                        double windSpeed = item.wind?.speed != null ? double.Parse(item.wind.speed.ToString()) : 0;

                        if (!dailyData.ContainsKey(dateStr))
                        {
                            dailyData[dateStr] = new DailyForecast
                            {
                                Date = date,
                                TempMax = temp,
                                TempMin = temp,
                                Condition = condition,
                                Description = description,
                                Icon = $"https://openweathermap.org/img/w/{icon}.png",
                                Humidity = humidity,
                                WindSpeed = windSpeed
                            };
                        }
                        else
                        {
                            var existing = dailyData[dateStr];
                            existing.TempMax = Math.Max(existing.TempMax, temp);
                            existing.TempMin = Math.Min(existing.TempMin, temp);
                        }
                    }
                }

                forecast.DailyForecasts = new System.Collections.Generic.List<DailyForecast>(dailyData.Values);
                return forecast;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Forecast API Error: {ex.Message}");
                return GetMockForecast(city, days);
            }
        }

        private WeatherData GetMockWeather(string city)
        {
            var random = new Random();
            return new WeatherData
            {
                City = city,
                Country = "PK",
                Temperature = random.Next(15, 35),
                FeelsLike = random.Next(15, 35),
                TempMin = random.Next(10, 25),
                TempMax = random.Next(25, 40),
                Condition = new[] { "Sunny", "Partly Cloudy", "Cloudy", "Clear" }[random.Next(4)],
                Description = "Mock weather data - API key not configured",
                Icon = "https://openweathermap.org/img/w/01d.png",
                Humidity = random.Next(30, 80),
                WindSpeed = random.Next(5, 25),
                Pressure = random.Next(1000, 1025),
                LastUpdated = DateTime.Now
            };
        }

        private WeatherForecast GetMockForecast(string city, int days)
        {
            var random = new Random();
            var forecast = new WeatherForecast { City = city, DailyForecasts = new System.Collections.Generic.List<DailyForecast>() };

            for (int i = 0; i < days; i++)
            {
                forecast.DailyForecasts.Add(new DailyForecast
                {
                    Date = DateTime.Now.AddDays(i),
                    TempMax = random.Next(25, 40),
                    TempMin = random.Next(10, 25),
                    Condition = new[] { "Sunny", "Partly Cloudy", "Cloudy" }[random.Next(3)],

                    Description = "Mock forecast data",
                    Icon = "https://openweathermap.org/img/w/01d.png",
                    Humidity = random.Next(30, 80),
                    WindSpeed = random.Next(5, 25)
                });
            }
            return forecast;
        }
    }
}