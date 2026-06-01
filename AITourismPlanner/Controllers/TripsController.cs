using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using AITourismPlanner.Services;
using AITourismPlanner.ViewModels;
namespace AITourismPlanner.Controllers
{
    public class TripsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIRecommendationService _aiService;
        private readonly IItineraryGenerator _itineraryGenerator;


        public TripsController(
            ApplicationDbContext context,
            IAIRecommendationService aiService,
            IItineraryGenerator itineraryGenerator)
        {
            _context = context;
            _aiService = aiService;
            _itineraryGenerator = itineraryGenerator;
        }


        // =========================================================
        // PLAN TRIP - AI Trip Planner
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> PlanTrip(int? destinationId = null)

        {
            var userId = HttpContext.Session.GetInt32("UserId");


            var model = new TripPlannerViewModel
            
            {
                Destinations = await _context.destinations
                    .Select(d => new { d.destination_id, d.name })
                    .ToListAsync(),
                SelectedDestinationId = destinationId,
                StartDate = DateTime.Now.AddDays(7),
                EndDate = DateTime.Now.AddDays(10),
                Budget = 50000,
                Travelers = 2
            };

            // Get AI recommendations for logged-in users
            if (userId.HasValue)
            {
                model.Recommendations = await _aiService.GetRecommendationsAsync(userId.Value);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlanTrip(TripPlannerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get best destination using AI if not selected
                int destinationId = model.SelectedDestinationId ?? 0;
                string destinationName = "";

                if (destinationId == 0 && !string.IsNullOrEmpty(model.Interests))
                {
                    var bestMatch = await _aiService.GetBestMatchAsync(
                        model.Budget ?? 50000,
                        (model.EndDate - model.StartDate).Days + 1,
                        model.Interests
                    );

                    if (bestMatch != null)
                    {
                        destinationId = bestMatch.destination_id;
                        destinationName = bestMatch.name;
                    }
                }

                if (destinationId == 0)
                {
                    ModelState.AddModelError("", "Please select a destination or provide interests for AI recommendation");
                    model.Destinations = await _context.destinations
                        .Select(d => new { d.destination_id, d.name })
                        .ToListAsync();
                    return View(model);
                }

                // Generate trip plan
                var trip = await _itineraryGenerator.GenerateTripPlanAsync(
                    userId.Value,
                    destinationId,
                    model.StartDate,
                    model.EndDate,
                    model.Budget ?? 50000
                );

                // Save AI recommendation
                await _aiService.SaveRecommendationAsync(
                    userId.Value,
                    destinationName,
                    $"AI recommended based on budget of PKR {model.Budget:N0} and interests: {model.Interests}"
                );

                TempData["Success"] = "Your AI trip plan has been created!";
                return RedirectToAction("Details", new { id = trip.trip_id });
            }

            model.Destinations = await _context.destinations
                .Select(d => new { d.destination_id, d.name })
                .ToListAsync();
            return View(model);
        }

        // =========================================================
        // TRIP DETAILS
        // =========================================================
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var trip = await _context.trips
                .Include(t => t.Destination)
                .Include(t => t.Itineraries)
                .FirstOrDefaultAsync(t => t.trip_id == id);

            if (trip == null)
            {
                return NotFound();
            }

            // Check if user owns this trip
            if (trip.user_id != userId)
            {
                return Forbid();
            }

            return View(trip);
        }

        // =========================================================
        // MY TRIPS
        // =========================================================
        public async Task<IActionResult> MyTrips()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var trips = await _context.trips
                .Include(t => t.Destination)
                .Where(t => t.user_id == userId)
                .OrderByDescending(t => t.created_at)
                .ToListAsync();

            return View(trips);
        }

        // =========================================================
        // DELETE TRIP
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var trip = await _context.trips.FindAsync(id);

            if (trip != null && trip.user_id == userId)
            {
                _context.trips.Remove(trip);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Trip deleted successfully";
            }

            return RedirectToAction("MyTrips");
        }
    }
}