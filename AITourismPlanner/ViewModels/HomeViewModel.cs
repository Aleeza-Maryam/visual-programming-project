using AITourismPlanner.Models;
using AITourismPlanner.Controllers;

namespace AITourismPlanner.ViewModels
{
    public class HomeViewModel
    {
        public List<Destination> PopularDestinations { get; set; } = new();
        public List<Hotel> FeaturedHotels { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Review> Testimonials { get; set; } = new();
        public List<UserReviewViewModel> UserReviews { get; set; } = new();
    }
}