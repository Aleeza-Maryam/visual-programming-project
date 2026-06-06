using AITourismPlanner.Data;
using AITourismPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITourismPlanner.Services
{
    public class AITourismService : IAITourismService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ModelTrainingService _searchModel;
        private readonly Random _random;

        public AITourismService(ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _cache = memoryCache;
            _searchModel = new ModelTrainingService();
            _random = new Random();

            var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "training_data.csv");
            if (File.Exists(dataPath))
            {
                _searchModel.TrainModel(dataPath);
            }
        }

        // =========================================================
        // FEATURE 1: SMART SEARCH
        // =========================================================
        public async Task<List<Package>> SmartSearch(string query)
        {
            if (string.IsNullOrEmpty(query))
                return new List<Package>();

            try
            {
                var prediction = _searchModel.Predict(query.ToLower());

                var packages = await _context.packages
                    .Where(p => p.is_active &&
                        (p.package_type == prediction.PredictedCategory ||
                         p.destination_name.Contains(prediction.PredictedDestination) ||
                         p.package_name.ToLower().Contains(query.ToLower())))
                    .Take(6)
                    .ToListAsync();

                if (!packages.Any())
                {
                    packages = await _context.packages
                        .Where(p => p.is_active)
                        .Take(6)
                        .ToListAsync();
                }

                return packages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SmartSearch Error: {ex.Message}");
                return new List<Package>();
            }
        }

        // =========================================================
        // FEATURE 2: SENTIMENT ANALYSIS
        // =========================================================
        public async Task<string> AnalyzeReviewSentiment(string reviewText)
        {
            if (string.IsNullOrEmpty(reviewText))
                return "Neutral";

            return await Task.Run(() =>
            {
                var positiveWords = new[] { "good", "great", "excellent", "amazing", "wonderful", "beautiful", "best", "love", "perfect", "fantastic" };
                var negativeWords = new[] { "bad", "poor", "terrible", "awful", "disappointing", "worst", "hate", "waste", "horrible" };

                var text = reviewText.ToLower();
                var positiveCount = positiveWords.Count(w => text.Contains(w));
                var negativeCount = negativeWords.Count(w => text.Contains(w));

                if (positiveCount > negativeCount)
                    return "Positive";
                else if (negativeCount > positiveCount)
                    return "Negative";
                else
                    return "Neutral";
            });
        }

        // =========================================================
        // FEATURE 3: BUDGET COMPARISON
        // =========================================================
        public async Task<BudgetComparison> CompareBudget(string dest1, string dest2, int days)
        {
            var result = new BudgetComparison
            {
                Destination1 = dest1,
                Destination2 = dest2
            };

            try
            {
                var package1 = await _context.packages.FirstOrDefaultAsync(p => p.destination_name.Contains(dest1) && p.is_active);
                var package2 = await _context.packages.FirstOrDefaultAsync(p => p.destination_name.Contains(dest2) && p.is_active);

                result.Cost1 = (package1?.price_per_person ?? 15000) * days;
                result.Cost2 = (package2?.price_per_person ?? 15000) * days;

                if (result.Cost1 < result.Cost2)
                {
                    result.Recommendation = dest1;
                    result.Savings = result.Cost2 - result.Cost1;
                }
                else
                {
                    result.Recommendation = dest2;
                    result.Savings = result.Cost1 - result.Cost2;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CompareBudget Error: {ex.Message}");
            }

            return result;
        }

        // =========================================================
        // FEATURE 4: PERSONALIZED RECOMMENDATIONS
        // =========================================================
        public async Task<List<Package>> PersonalizedRecommendations(int userId)
        {
            var cacheKey = $"recs_{userId}";

            if (_cache.TryGetValue(cacheKey, out List<Package> cachedRecs))
                return cachedRecs ?? new List<Package>();

            try
            {
                var userBookings = await _context.package_bookings
                    .Where(b => b.user_id == userId)
                    .Include(b => b.Package)
                    .ToListAsync();

                var preferredType = userBookings
                    .Where(b => b.Package != null)
                    .Select(b => b.Package!.package_type)
                    .FirstOrDefault();

                List<Package> recommendations;

                if (!string.IsNullOrEmpty(preferredType))
                {
                    recommendations = await _context.packages
                        .Where(p => p.package_type == preferredType && p.is_active)
                        .Take(4)
                        .ToListAsync();
                }
                else
                {
                    recommendations = await _context.packages
                        .Where(p => p.is_active)
                        .Take(4)
                        .ToListAsync();
                }

                _cache.Set(cacheKey, recommendations, TimeSpan.FromHours(1));
                return recommendations;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Recommendations Error: {ex.Message}");
                return new List<Package>();
            }
        }

        // =========================================================
        // FEATURE 5: ITINERARY GENERATOR
        // =========================================================
        public async Task<string> GenerateItinerary(string destination, int days, string budget)
        {
            var budgetLower = budget.ToLower();
            var dailyBudget = budgetLower.Contains("low") ? 5000 :
                              budgetLower.Contains("medium") ? 10000 : 20000;

            var itinerary = new StringBuilder();
            itinerary.AppendLine($"=== {days}-Day Itinerary for {destination} ===\n");
            itinerary.AppendLine($"Budget: {budget} | Daily Budget: PKR {dailyBudget:N0}\n");

            var activities = GetActivitiesForDestination(destination);

            for (int day = 1; day <= days; day++)
            {
                itinerary.AppendLine($"Day {day}:");
                var activity = activities[_random.Next(activities.Count)];
                itinerary.AppendLine($"  Morning: {activity.Morning}");
                itinerary.AppendLine($"  Afternoon: {activity.Afternoon}");
                itinerary.AppendLine($"  Evening: {activity.Evening}");
                itinerary.AppendLine($"  Estimated Cost: PKR {dailyBudget:N0}");
                itinerary.AppendLine();
            }

            itinerary.AppendLine("=== Tips ===");
            itinerary.AppendLine("- Book hotels in advance for better rates");
            itinerary.AppendLine("- Carry warm clothes if visiting in winter");
            itinerary.AppendLine("- Try local food for authentic experience");

            return await Task.FromResult(itinerary.ToString());
        }

        // =========================================================
        // FEATURE 6: AI CHATBOT
        // =========================================================
        public async Task<string> ChatbotResponse(string userQuestion)
        {
            if (string.IsNullOrEmpty(userQuestion))
                return "Please ask me something about travel!";

            var question = userQuestion.ToLower();

            var faqAnswer = GetFaqAnswer(question);
            if (!string.IsNullOrEmpty(faqAnswer))
                return faqAnswer;

            if (question.Contains("hello") || question.Contains("hi") || question.Contains("assalam"))
            {
                return "Assalam-o-Alaikum! I'm your AI travel assistant. How can I help plan your trip?";
            }

            if (question.Contains("help"))
            {
                return "I can help you with:\n- Finding best destinations\n- Budget planning\n- Hotel recommendations\n- Transport options\n- Weather information\n- Package deals\n\nJust ask me anything about travel!";
            }

            if (question.Contains("budget"))
            {
                var words = question.Split(' ');
                foreach (var word in words)
                {
                    if (int.TryParse(word, out int budget))
                    {
                        var packages = await _context.packages
                            .Where(p => p.price_per_person <= budget && p.is_active)
                            .Take(3)
                            .ToListAsync();

                        if (packages.Any())
                        {
                            var response = $"With PKR {budget:N0} budget, you can consider:\n";
                            foreach (var pkg in packages)
                            {
                                response += $"• {pkg.package_name} (PKR {pkg.price_per_person:N0})\n";
                            }
                            return response;
                        }
                        return $"No packages found under PKR {budget:N0}. Try increasing your budget!";
                    }
                }
                return "Please tell me your budget amount (e.g., 'budget 50000')";
            }

            if (question.Contains("weather"))
            {
                return "To check weather, please visit the Weather page or go to any destination details page for current forecast!";
            }

            if (question.Contains("hotel"))
            {
                return "You can find hotels on our Real Hotels page! We have partnerships with Serena, Pearl Continental, Marriott, and more.";
            }

            if (question.Contains("package") || question.Contains("tour"))
            {
                var packages = await _context.packages.Where(p => p.is_active).Take(3).ToListAsync();
                var response = "Here are our top packages:\n";
                foreach (var pkg in packages)
                {
                    response += $"• {pkg.package_name} - PKR {pkg.price_per_person:N0} ({pkg.duration_days} days)\n";
                }
                return response;
            }

            if (question.Contains("thank"))
            {
                return "You're welcome! Happy travels! If you need anything else, just ask.";
            }

            return "Thanks for your message! I can help you with destination recommendations, budget planning, hotel bookings, or transport options. What would you like to know?";
        }

        private string GetFaqAnswer(string question)
        {
            var faqs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "best time to visit hunza", "Best time to visit Hunza is May to October when the weather is pleasant and fruits are in season." },
                { "best time to visit murree", "Best time to visit Murree is March to October. December to February for snow lovers!" },
                { "best time to visit skardu", "Best time to visit Skardu is June to September for trekking and lake visits." },
                { "how to reach hunza", "You can reach Hunza by air (flight to Gilgit then drive) or by road via Karakoram Highway from Islamabad (about 14-16 hours)." },
                { "how to reach skardu", "Take a flight from Islamabad to Skardu (1 hour) or drive via KKH (24 hours) with beautiful scenery." },
                { "cheapest destination", "Murree is the most budget-friendly option, followed by Naran and Swat." },
                { "luxury destination", "Skardu and Hunza offer luxury resorts with premium packages." },
                { "family friendly", "Murree and Naran are great for family trips with easy activities and good hotels." },
                { "honeymoon", "Swat, Hunza, and Skardu are perfect for honeymoon with romantic settings and luxury stays." },
                { "adventure", "Skardu, Fairy Meadows, and Hunza are best for adventure lovers with trekking and jeep safaris." }
            };

            foreach (var faq in faqs)
            {
                if (question.Contains(faq.Key))
                    return faq.Value;
            }

            return null;
        }

        private List<(string Morning, string Afternoon, string Evening)> GetActivitiesForDestination(string destination)
        {
            var destLower = destination.ToLower();

            if (destLower.Contains("hunza"))
            {
                return new List<(string, string, string)>
                {
                    ("Visit Baltit Fort", "Explore Karimabad Bazaar", "Sunset at Eagle's Nest"),
                    ("Drive to Attabad Lake", "Boat ride on the lake", "Dinner with mountain view"),
                    ("Visit Altit Fort", "Walk through old Hunza", "Local cultural show")
                };
            }
            else if (destLower.Contains("murree"))
            {
                return new List<(string, string, string)>
                {
                    ("Walk on Mall Road", "Visit Kashmir Point", "Shopping at Mall Road"),
                    ("Ride Patriata Chairlift", "Visit Pindi Point", "Sunset view"),
                    ("Visit Bhurban", "Nature walk in pine forest", "Dinner at Monal")
                };
            }
            else if (destLower.Contains("skardu"))
            {
                return new List<(string, string, string)>
                {
                    ("Visit Shangrila Lake", "Explore Katpana Desert", "Sunset at Cold Desert"),
                    ("Drive to Satpara Lake", "Boating and fishing", "Stargazing"),
                    ("Visit Manthokha Waterfall", "Picnic by the waterfall", "Local dinner")
                };
            }
            else
            {
                return new List<(string, string, string)>
                {
                    ("Sightseeing", "Local exploration", "Dinner at local restaurant"),
                    ("Visit main attractions", "Shopping", "Cultural experience"),
                    ("Nature walk", "Photography", "Relaxation")
                };
            }
        }
    }
}