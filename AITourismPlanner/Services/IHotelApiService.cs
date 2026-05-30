using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AITourismPlanner.Services
{
    public interface IHotelApiService
    {
        Task<List<HotelModel>> GetNearbyHotelsAsync(string city, DateTime checkIn, DateTime checkOut, int guests = 2);
    }

    public class HotelModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Rating { get; set; }
        public decimal? PricePerNight { get; set; }
        public string Currency { get; set; } = "PKR";
        public string ImageUrl { get; set; } = string.Empty;
        public int? ReviewCount { get; set; }
        public string BookingLink { get; set; } = string.Empty;
        public List<string> Amenities { get; set; } = new();
    }
}