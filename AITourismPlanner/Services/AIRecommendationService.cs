using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;

namespace AITourismPlanner.Services
{
    public class AIRecommendationService : IAIRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIRecommendationService> _logger;

        public AIRecommendationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AIRecommendationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // =========================================================
        // METHOD 1: Get recommendations by budget and category
        // =========================================================
        public async Task<List<Destination>> GetRecommendationsAsync(int userId, decimal? budget = null, string category = null)
        {
            try
            {
                var query = _context.destinations
                    .Include(d => d.Category)
                    .AsQueryable();

                // Filter by budget if provided
                if (budget.HasValue && budget.Value > 0)
                {
                    query = query.Where(d => d.estimated_cost <= budget.Value);
                }

                // Filter by category if provided
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(d => d.Category != null && d.Category.category_name == category);
                }

                // Get top 5 destinations by rating
                var destinations = await query
                    .OrderByDescending(d => d.rating_average)
                    .Take(5)
                    .ToListAsync();

                return destinations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations for user {UserId}", userId);
                return new List<Destination>();
            }
        }

        // =========================================================
        // METHOD 2: Get best match using AI scoring algorithm
        // =========================================================
        public async Task<Destination?> GetBestMatchAsync(decimal budget, int days, string interests)
        {
            try
            {
                // Calculate per-day budget
                decimal perDayBudget = budget / days;

                // Get all destinations within budget
                var destinations = await _context.destinations
                    .Include(d => d.Category)
                    .Where(d => d.estimated_cost.HasValue && d.estimated_cost <= perDayBudget * 1.2m) // 20% margin
                    .ToListAsync();

                if (!destinations.Any())
                {
                    return null;
                }

                // AI Scoring Algorithm
                var scoredDestinations = new List<(Destination dest, decimal score, string reason)>();

                foreach (var dest in destinations)
                {
                    decimal score = 0;
                    List<string> matchReasons = new List<string>();

                    // 1. BUDGET SCORE (40% weight)
                    if (dest.estimated_cost.HasValue)
                    {
                        decimal budgetRatio = dest.estimated_cost.Value / perDayBudget;
                        if (budgetRatio <= 1)
                        {
                            // Destination fits in budget
                            decimal budgetScore = 40 * (1 - (budgetRatio * 0.5m));
                            score += budgetScore;
                            matchReasons.Add($"Fits your budget of Rs.{perDayBudget:N0}/day");
                        }
                        else if (budgetRatio <= 1.2m)
                        {
                            // Slightly over budget but still acceptable
                            score += 20;
                            matchReasons.Add($"Slightly above your budget ({(budgetRatio - 1) * 100:F0}% more)");
                        }
                    }

                    // 2. RATING SCORE (30% weight)
                    decimal ratingScore = (dest.rating_average / 5) * 30;
                    score += ratingScore;
                    if (dest.rating_average >= 4)
                    {
                        matchReasons.Add($"Highly rated ({dest.rating_average}/5 stars)");
                    }

                    // 3. INTEREST MATCH SCORE (30% weight)
                    if (!string.IsNullOrEmpty(interests) && dest.Category != null)
                    {
                        string[] interestKeywords = interests.ToLower().Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string categoryName = dest.Category.category_name.ToLower();

                        foreach (var keyword in interestKeywords)
                        {
                            if (categoryName.Contains(keyword) ||
                                (keyword == "mountain" && categoryName == "mountains") ||
                                (keyword == "history" && categoryName == "historical"))
                            {
                                score += 30;
                                matchReasons.Add($"Matches your interest in {dest.Category.category_name}");
                                break;
                            }
                        }
                    }

                    scoredDestinations.Add((dest, score, string.Join(", ", matchReasons)));
                }

                // Get best match
                var bestMatch = scoredDestinations.OrderByDescending(x => x.score).FirstOrDefault();

                if (bestMatch.dest != null && bestMatch.score > 0)
                {
                    _logger.LogInformation("Best match found: {Destination} with score {Score}", bestMatch.dest.name, bestMatch.score);
                }

                return bestMatch.dest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding best match for budget {Budget}, days {Days}", budget, days);
                return null;
            }
        }

        // =========================================================
        // METHOD 3: Save recommendation to database
        // =========================================================
        public async Task SaveRecommendationAsync(int userId, string destinationName, string reason)
        {
            try
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

                _logger.LogInformation("Saved recommendation for user {UserId}: {Destination}", userId, destinationName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving recommendation for user {UserId}", userId);
            }
        }

        // =========================================================
        // METHOD 4: Get personalized recommendations using user history
        // =========================================================
        public async Task<List<Destination>> GetPersonalizedRecommendationsAsync(int userId, int maxResults = 5)
        {
            try
            {
                // Get user preferences
                var userPref = await _context.user_preferences
                    .FirstOrDefaultAsync(up => up.user_id == userId);

                // Get user's past bookings/trips
                var pastTrips = await _context.trips
                    .Where(t => t.user_id == userId)
                    .Select(t => t.destination_id)
                    .ToListAsync();

                var pastBookings = await _context.hotel_bookings
                    .Include(hb => hb.Hotel)
                    .Where(hb => hb.user_id == userId && hb.Hotel != null)
                    .Select(hb => hb.Hotel.destination_id)
                    .ToListAsync();

                var visitedDestinationIds = pastTrips.Union(pastBookings).Distinct().ToList();

                // Build recommendation query
                var query = _context.destinations
                    .Include(d => d.Category)
                    .Where(d => !visitedDestinationIds.Contains(d.destination_id)); // Exclude visited places

                // Apply user preferences
                if (userPref != null)
                {
                    if (userPref.preferred_budget.HasValue)
                    {
                        query = query.Where(d => d.estimated_cost <= userPref.preferred_budget.Value);
                    }

                    if (!string.IsNullOrEmpty(userPref.favorite_category))
                    {
                        query = query.Where(d => d.Category != null && d.Category.category_name == userPref.favorite_category);
                    }
                }

                // Get top recommendations
                var recommendations = await query
                    .OrderByDescending(d => d.rating_average)
                    .Take(maxResults)
                    .ToListAsync();

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations for user {UserId}", userId);
                return new List<Destination>();
            }
        }
    }
}