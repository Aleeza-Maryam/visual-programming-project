using AITourismPlanner.Models;

namespace AITourismPlanner.Services
{
    public interface IAIRecommendationService
    {
        Task<List<Destination>> GetRecommendationsAsync(int userId, decimal? budget = null, string category = null);
        Task<Destination> GetBestMatchAsync(decimal budget, int days, string interests);
        Task SaveRecommendationAsync(int userId, string destinationName, string reason);
    }
}