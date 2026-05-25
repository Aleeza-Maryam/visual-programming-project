using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;

namespace AITourismPlanner.Services
{
    public class AIRecommendationService : IAIRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AIRecommendationService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<List<Destination>> GetRecommendationsAsync(int userId, decimal? budget = null, decimal? category = null)
        {
            var query = _context.destinations
                .Include(d => d.Category)
                .AsQueryable();

            if (budget.HasValue)
            {
                query = query.Where(d => d.estimated_cost <= budget.Value);
            }

            var destinations = await query
                .OrderByDescending(d => d.rating_average)
                .Take(5)
                .ToListAsync();

            return destinations;
        }

        public async Task<Destination> GetBestMatchAsync(decimal budget, int days, string interests)
        {
            var destinations = await _context.destinations
                .Include(d => d.Category)
                .Where(d => d.estimated_cost <= budget / days)
                .ToListAsync();

            // AI Scoring Algorithm
            var scoredDestinations = new List<(Destination dest, decimal score)>();

            foreach (var dest in destinations)
            {
                decimal score = 0;

                // Budget score (40% weight)
                if (dest.estimated_cost.HasValue)
                {
                    decimal budgetRatio = (decimal)dest.estimated_cost / (budget / days);
                    if (budgetRatio <= 1)
                        score += 40 * (1 - budgetRatio);
                }

                // Rating score (30% weight)
                score += (dest.rating_average / 5) * 30;

                // Interest match score (30% weight)
                if (!string.IsNullOrEmpty(interests) && dest.Category != null)
                {
                    if (interests.ToLower().Contains(dest.Category.category_name.ToLower()))
                        score += 30;
                }

                scoredDestinations.Add((dest, score));
            }

            return scoredDestinations.OrderByDescending(x => x.score).FirstOrDefault().dest;
        }

        public async Task SaveRecommendationAsync(int userId, string destinationName, string reason)
        {
            var recommendation = new AIRecommendation
            {
                user_id = userId,
                recommended_destination = destinationName,
                recommendation_reason = reason,
                generated_at = DateTime.Now
            };

            _context.ai_recommendations.Add(recommendation);
            await _context.SaveChangesAsync();
        }
    }
}