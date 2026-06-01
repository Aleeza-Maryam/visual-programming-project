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
                // Get Testimonials
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

                var viewModel = new HomeViewModel
                {
                    PopularDestinations = popularDestinations,
                    FeaturedHotels = featuredHotels,
                    Categories = categories,
                    Testimonials = testimonials
                };

                return View(viewModel);
            }
            catch (Exception ex)
            
            {
                return View(new HomeViewModel
                {
                    PopularDestinations = new List<Destination>(),
                    FeaturedHotels = new List<Hotel>(),
                    Categories = new List<Category>(),
                    Testimonials = new List<Review>()
                });
            }
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
}