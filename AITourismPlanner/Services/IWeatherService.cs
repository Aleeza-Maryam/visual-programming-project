using AITourismPlanner.Models;

namespace AITourismPlanner.Services
{
    public interface IWeatherService
    {
        Task<Weather> GetWeatherForDestinationAsync(int destinationId);
        Task UpdateWeatherDataAsync();
    }
}