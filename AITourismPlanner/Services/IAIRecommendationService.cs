using AITourismPlanner.Models;

namespace AITourismPlanner.Services
{
    public interface IAIRecommendationService
    {
        // Method 1: Get recommendations based on user preferences
        Task<List<Destination>> GetRecommendationsAsync(int userId, decimal? budget = null, string category = null);

        // Method 2: Get best match based on budget, days, interests
        Task<Destination?> GetBestMatchAsync(decimal budget, int days, string interests);

        // Method 3: Save recommendation to database
        Task SaveRecommendationAsync(int userId, string destinationName, string reason);

        // Method 4: Get personalized recommendations using AI scoring
        Task<List<Destination>> GetPersonalizedRecommendationsAsync(int userId, int maxResults = 5);
    }
}