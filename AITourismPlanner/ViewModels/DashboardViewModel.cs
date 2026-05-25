using AITourismPlanner.Models;

namespace AITourismPlanner.ViewModels
{
    public class DashboardViewModel
    {
        public User User { get; set; }
        public List<Trip> UpcomingTrips { get; set; }
        public List<HotelBooking> RecentBookings { get; set; }
        public int TotalTrips { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public int WishlistCount { get; set; }
        public List<AIRecommendation> Recommendations { get; set; }
        public List<Notification> Notifications { get; set; }
    }
}