using AITourismPlanner.Models;
using AITourismPlanner.Services;
using System.Security.Cryptography.Xml;

namespace AITourismPlanner.ViewModels
{
    public class ApiDestinationViewModel
    {
        public string CityName { get; set; }
        public DestinationInfo DestinationInfo { get; set; }

        public WeatherData CurrentWeather { get; set; }
        public WeatherForecast Forecast { get; set; }
        public List<RealHotel> NearbyHotels { get; set; } = new();
        public List<Transport> Transports { get; set; } = new();
        public List<DestinationReview> Reviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}