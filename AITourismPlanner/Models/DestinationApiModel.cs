using System.Collections.Generic;

namespace AITourismPlanner.Models
{
    public class DestinationApiModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public decimal EstimatedCost { get; set; }
        public double Rating { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> ThingsToDo { get; set; } = new();
        public string BestTimeToVisit { get; set; }
        public string Weather { get; set; }
    }
}