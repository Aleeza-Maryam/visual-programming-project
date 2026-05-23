namespace AITourismPlanner.Models
{
    public class Trip
    {
        public int TripId { get; set; }

        public decimal TotalBudget { get; set; }

        public int TotalDays { get; set; }

        public string TripType { get; set; }
    }
}