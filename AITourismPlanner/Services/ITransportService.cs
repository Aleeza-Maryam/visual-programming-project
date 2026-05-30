using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AITourismPlanner.Services
{
    public interface ITransportService
    {
        Task<List<TransportOption>> GetTransportOptionsAsync(string from, string to, DateTime date);
    }

    public class TransportOption
    {
        public string Type { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Duration { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
    }
}