using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using AITourismPlanner.ViewModels;

namespace AITourismPlanner.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // =========================================================
                // Get Popular Destinations
                // =========================================================
                var popularDestinations = await _context.destinations
                    .OrderByDescending(d => d.rating_average)
                    .Take(6)
                    .Select(d => new Destination
                    {
                        destination_id = d.destination_id,
                        name = d.name == null ? "Unknown Destination" : d.name,
                        description = d.description == null ? "No description available" : d.description,
                        city = d.city == null ? "Pakistan" : d.city,
                        country = d.country == null ? "Pakistan" : d.country,
                        estimated_cost = d.estimated_cost == null ? 30000m : d.estimated_cost,
                        rating_average = d.rating_average == null ? 0m : d.rating_average,
                        thumbnail = d.thumbnail == null ? "/images/default-destination.jpg" : d.thumbnail,
                        best_season = d.best_season == null ? "All Year" : d.best_season,
                        category_id = d.category_id
                    })
                    .ToListAsync();

                // =========================================================
                // Get Featured Hotels
                // =========================================================
                var featuredHotels = await _context.hotels
                    .OrderByDescending(h => h.star_rating)
                    .Take(4)
                    .Select(h => new Hotel
                    {
                        hotel_id = h.hotel_id,
                        hotel_name = h.hotel_name == null ? "Unknown Hotel" : h.hotel_name,
                        star_rating = h.star_rating == null ? 3m : h.star_rating,
                        price_per_night = h.price_per_night == null ? 5000m : h.price_per_night,
                        image = h.image == null ? "/images/hotel-default.jpg" : h.image,
                        address = h.address == null ? "Main City" : h.address,
                        destination_id = h.destination_id
                    })
                    .ToListAsync();

                // =========================================================
                // Get Categories
                // =========================================================
                var categories = await _context.categories
                    .Select(c => new Category
                    {
                        category_id = c.category_id,
                        category_name = c.category_name == null ? "General" : c.category_name
                    })
                    .ToListAsync();

                // =========================================================
                // Get Testimonials (from reviews table)
                // =========================================================
                var testimonials = await _context.reviews
                    .OrderByDescending(r => r.review_date)
                    .Take(3)
                    .Select(r => new Review
                    {
                        review_id = r.review_id,
                        rating = r.rating == null ? 4 : r.rating,
                        review_text = r.review_text == null ? "Great experience!" : r.review_text,
                        review_date = r.review_date == null ? DateTime.Now : r.review_date,
                        user_id = r.user_id,
                        destination_id = r.destination_id
                    })
                    .ToListAsync();

                // =========================================================
                // Get User Reviews from destination_reviews table
                // =========================================================
                var userReviews = await _context.destination_reviews
                    .Include(r => r.User)
                    .Where(r => r.comment != null && r.comment != "")
                    .OrderByDescending(r => r.created_at)
                    .Take(6)
                    .Select(r => new UserReviewViewModel
                    {
                        ReviewId = r.review_id,
                        UserName = r.User != null ? r.User.full_name : "Anonymous",
                        DestinationName = r.destination_name,
                        Rating = r.rating ?? 0,
                        Comment = r.comment ?? "Great experience!",
                        CreatedAt = r.created_at
                    })
                    .ToListAsync();

                var viewModel = new HomeViewModel
                {
                    PopularDestinations = popularDestinations,
                    FeaturedHotels = featuredHotels,
                    Categories = categories,
                    Testimonials = testimonials,
                    UserReviews = userReviews
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HomeController Error: {ex.Message}");
                return View(new HomeViewModel
                {
                    PopularDestinations = new List<Destination>(),
                    FeaturedHotels = new List<Hotel>(),
                    Categories = new List<Category>(),
                    Testimonials = new List<Review>(),
                    UserReviews = new List<UserReviewViewModel>()
                });
            }
        }

        [HttpPost]
        public IActionResult SubscribeNewsletter(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Email is required" });

            if (!email.Contains("@") || !email.Contains("."))
                return Json(new { success = false, message = "Please enter a valid email address" });

            // Here you can save to database or send to email service
            // For now, just return success
            return Json(new { success = true, message = "Subscribed successfully!" });
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }

    // User Review ViewModel
    public class UserReviewViewModel
    {
        public int ReviewId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}