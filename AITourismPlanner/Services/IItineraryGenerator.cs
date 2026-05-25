using AITourismPlanner.Models;

namespace AITourismPlanner.Services
{
    public interface IItineraryGenerator
    {
        Task<Trip> GenerateTripPlanAsync(int userId, int destinationId, DateTime startDate, DateTime endDate, decimal budget);
        Task<List<Itinerary>> GenerateDayWiseItineraryAsync(int tripId, int destinationId, int days);
    }
}