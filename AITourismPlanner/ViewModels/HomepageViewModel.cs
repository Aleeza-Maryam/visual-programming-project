using AITourismPlanner.Models;
using AITourismPlanner.Services;
using System;
using System.Collections.Generic;

namespace AITourismPlanner.ViewModels
{
    public class HomepageViewModel
    {
        public List<DestinationApiModel> FeaturedDestinations { get; set; } = new();
        public List<DestinationApiModel> TrendingDestinations { get; set; } = new();
        public List<DestinationApiModel> SeasonalPicks { get; set; } = new();
        public List<ReviewViewModel> RecentReviews { get; set; } = new();
    }

    public class DestinationDetailViewModel
    {
        public DestinationApiModel Destination { get; set; } = new();
        public List<HotelModel> Hotels { get; set; } = new();
        public WeatherData CurrentWeather { get; set; } = new();
        //public WeatherForecastData WeatherForecast { get; set; } = new();

        //public List<TransportOption> TransportOptions { get; set; } = new();
        public List<ReviewViewModel> Reviews { get; set; } = new();

        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
    }

    public class ReviewViewModel
    {
        public int ReviewId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool CanEdit { get; set; }
    }
}