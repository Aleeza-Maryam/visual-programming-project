using AITourismPlanner.Models;

namespace AITourismPlanner.ViewModels
{
    public class HomeViewModel
    {
        public List<Destination> PopularDestinations { get; set; } = new List<Destination>();
        public List<Hotel> FeaturedHotels { get; set; } = new List<Hotel>();
        public List<Category> Categories { get; set; } = new List<Category>();

        public List<Review> Testimonials { get; set; } = new List<Review>();
    }
}