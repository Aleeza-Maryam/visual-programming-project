using System.Text.Json;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace AITourismPlanner.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public WeatherService(ApplicationDbContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<Weather> GetWeatherForDestinationAsync(int destinationId)
        {
            var weather = await _context.weather
                .Include(w => w.Destination)
                .FirstOrDefaultAsync(w => w.destination_id == destinationId);

            if (weather == null || weather.updated_at < DateTime.Now.AddHours(-3))
            {
                await UpdateWeatherForDestination(destinationId);
                weather = await _context.weather
                    .FirstOrDefaultAsync(w => w.destination_id == destinationId);
            }

            return weather;
        }

        private async Task UpdateWeatherForDestination(int destinationId)
        {
            var destination = await _context.destinations.FindAsync(destinationId);
            if (destination == null) return;

            var apiKey = _configuration["WeatherAPI:Key"];
            var baseUrl = _configuration["WeatherAPI:BaseUrl"];

            // Simulated weather data (in production, call actual API)
            var random = new Random();
            var weather = new Weather
            {
                destination_id = destinationId,
                temperature = random.Next(15, 35),
                weather_condition = new[] { "Sunny", "Cloudy", "Rainy", "Clear" }[random.Next(4)],
                humidity = random.Next(40, 80),
                updated_at = DateTime.Now
            };

            var existingWeather = await _context.weather
                .FirstOrDefaultAsync(w => w.destination_id == destinationId);

            if (existingWeather != null)
            {
                existingWeather.temperature = weather.temperature;
                existingWeather.weather_condition = weather.weather_condition;
                existingWeather.humidity = weather.humidity;
                existingWeather.updated_at = weather.updated_at;
            }
            else
            {
                _context.weather.Add(weather);
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateWeatherDataAsync()
        {
            var destinations = await _context.destinations.ToListAsync();
            foreach (var destination in destinations)
            {
                await UpdateWeatherForDestination(destination.destination_id);
            }
        }
    }
}