using AITourismPlanner.Data;
using AITourismPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace AITourismPlanner.Services
{
    public class ItineraryGenerator : IItineraryGenerator
    {
        private readonly ApplicationDbContext _context;

        public ItineraryGenerator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Trip> GenerateTripPlanAsync(int userId, int destinationId, DateTime startDate, DateTime endDate, decimal budget)
        {
            var days = (endDate - startDate).Days;
            var destination = await _context.destinations.FindAsync(destinationId);

            var trip = new Trip
            {
                user_id = userId,
                total_budget = budget,
                total_days = days + 1,
                start_date = startDate,
                end_date = endDate,
                created_at = DateTime.Now
            };

            _context.trips.Add(trip);
            await _context.SaveChangesAsync();

            await GenerateDayWiseItineraryAsync(trip.trip_id, destinationId, days + 1);

            return trip;
        }

        public async Task<List<Itinerary>> GenerateDayWiseItineraryAsync(int tripId, int destinationId, int days)
        {
            var destination = await _context.destinations
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.destination_id == destinationId);

            var itineraries = new List<Itinerary>();

            if (destination == null) return itineraries;

            var activities = GetActivitiesForDestination(destination.name, destination.Category?.category_name);

            // Safe budget calculation: Agar estimated_cost null ho tou default value lag jaye
            var estimatedCost = destination.estimated_cost ?? 1000m;
            var dailyBudget = estimatedCost / days;

            for (int day = 1; day <= days; day++)
            {
                var activityIndex = (day - 1) % activities.Count;
                var activity = activities[activityIndex];

                var itinerary = new Itinerary
                {
                    trip_id = tripId,
                    day_number = day,
                    activity = $"{activity.Name} - {activity.Description}",
                    location = destination.name,
                    estimated_cost = dailyBudget * 0.3m, // 30% of daily cost for specific activity
                    activity_time = activity.Time,
                    duration = activity.Duration
                };

                itineraries.Add(itinerary);
                _context.itineraries.Add(itinerary);
            }

            await _context.SaveChangesAsync();
            return itineraries;
        }

        private List<(string Name, string Description, string Time, string Duration)> GetActivitiesForDestination(string destName, string category)
        {
            var activities = new List<(string Name, string Description, string Time, string Duration)>();

            // Standardizing string check to prevent case mismatches
            var normalizedCategory = category?.Trim().ToLower() ?? "general";

            if (normalizedCategory == "mountains")
            {
                activities.Add(("Morning Trek", "Start your day with a refreshing trek", "07:00 AM", "3 hours"));
                activities.Add(("Scenic Viewpoint", "Visit the best viewpoints", "10:00 AM", "2 hours"));
                activities.Add(("Local Lunch", "Enjoy local cuisine", "12:30 PM", "1.5 hours"));
                activities.Add(("Photography Session", "Capture beautiful landscapes", "02:00 PM", "2 hours"));
                activities.Add(("Sunset Point", "Watch the stunning sunset", "05:00 PM", "1.5 hours"));
                activities.Add(("Dinner", "Traditional dinner", "07:00 PM", "1 hour"));
            }
            else if (normalizedCategory == "beach")
            {
                activities.Add(("Sunrise Walk", "Peaceful beach walk at sunrise", "06:30 AM", "1.5 hours"));
                activities.Add(("Water Sports", "Scuba diving, jet-skiing or surfing", "09:30 AM", "3 hours"));
                activities.Add(("Beachfront Seafood", "Fresh lunch by the shore", "01:00 PM", "1.5 hours"));
                activities.Add(("Sunbathing & Relaxing", "Unwind under beach umbrellas", "03:00 PM", "2 hours"));
                activities.Add(("Cruise Tour", "Evening boat ride to see the sunset", "05:30 PM", "2 hours"));
                activities.Add(("Beach Bonfire", "Night dinner with music and bonfire", "08:00 PM", "2.5 hours"));
            }
            else if (normalizedCategory == "historical" || normalizedCategory == "culture")
            {
                activities.Add(("Museum Tour", "Explore local history museum", "09:00 AM", "2.5 hours"));
                activities.Add(("Heritage Site Walk", "Guided tour of ancient architecture", "11:30 AM", "2 hours"));
                activities.Add(("Traditional Marketplace", "Shop local crafts and souvenirs", "02:30 PM", "2 hours"));
                activities.Add(("Cultural Show", "Watch local folk dance or theater", "06:00 PM", "1.5 hours"));
                activities.Add(("Authentic Dining", "Try historical recipes and food", "07:30 PM", "1.5 hours"));
            }
            else // Default activities fallback for city or any other category
            {
                activities.Add(("City Sightseeing", "Hop-on hop-off city bus tour", "09:00 AM", "3 hours"));
                activities.Add(("Local Shopping", "Visit the famous malls/bazaars", "01:00 PM", "2 hours"));
                activities.Add(("Park Relaxation", "Leisure time at the central city park", "04:00 PM", "1.5 hours"));
                activities.Add(("Fine Dining", "Enjoy top-rated restaurant dinner", "07:30 PM", "2 hours"));
            }

            return activities;
        }
    }
}