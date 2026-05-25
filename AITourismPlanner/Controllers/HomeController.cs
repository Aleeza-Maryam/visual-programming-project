using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using System.Diagnostics;

namespace AITourismPlanner.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel
            {
                PopularDestinations = await _context.destinations
                    .Include(d => d.Category)
                    .Where(d => d.rating_average >= 4)
                    .Take(6)
                    .ToListAsync(),

                FeaturedHotels = await _context.hotels
                    .Include(h => h.Destination)
                    .OrderByDescending(h => h.star_rating)
                    .Take(4)
                    .ToListAsync(),

                Categories = await _context.categories.ToListAsync(),

                Testimonials = await _context.reviews
                    .Include(r => r.User)
                    .Include(r => r.Destination)
                    .Where(r => r.rating >= 4)
                    .OrderByDescending(r => r.review_date)
                    .Take(3)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class HomeViewModel
    {
        public List<Destination> PopularDestinations { get; set; }
        public List<Hotel> FeaturedHotels { get; set; }
        public List<Category> Categories { get; set; }
        public List<Review> Testimonials { get; set; }
    }
}