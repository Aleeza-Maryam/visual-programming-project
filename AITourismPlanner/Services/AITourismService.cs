using AITourismPlanner.Data;
using AITourismPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private readonly HttpClient _httpClient;
        private readonly string _groqApiKey;
        private readonly string _groqModel = "meta-llama/llama-4-scout-17b-16e-instruct";

        public AITourismService(ApplicationDbContext context, IMemoryCache memoryCache, IConfiguration configuration)
        {
            _context = context;
            _cache = memoryCache;
            _searchModel = new ModelTrainingService();
            _random = new Random();
            _httpClient = new HttpClient();
            _groqApiKey = configuration["Groq:ApiKey"];

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
        // FEATURE 6: AI CHATBOT - GROQ + LLAMA 4
        // =========================================================
        public async Task<string> ChatbotResponse(string userQuestion)
        {
            if (string.IsNullOrEmpty(userQuestion))
                return "Please ask me something about travel!";

            try
            {
                // First check FAQs for instant response
                var faqAnswer = GetFaqAnswer(userQuestion);
                if (!string.IsNullOrEmpty(faqAnswer))
                    return faqAnswer;

                // If no FAQ match, use Groq Llama 4
                if (!string.IsNullOrEmpty(_groqApiKey) && _groqApiKey != "gsk_YOUR_API_KEY_HERE")
                {
                    var requestBody = new
                    {
                        model = _groqModel,
                        messages = new[]
                        {
                            new {
                                role = "system",
                                content = "You are a helpful Pakistan travel assistant. Answer questions about destinations (Hunza, Murree, Skardu, Naran, Swat, Lahore, Islamabad), hotels, transport, best time to visit, tour packages, and travel tips in Pakistan. Keep responses concise (2-3 sentences), friendly, and helpful."
                            },
                            new { role = "user", content = userQuestion }
                        },
                        temperature = 0.7,
                        max_tokens = 300
                    };

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri("https://api.groq.com/openai/v1/chat/completions"),
                        Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("Authorization", $"Bearer {_groqApiKey}");

                    var response = await _httpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                    dynamic data = JsonConvert.DeserializeObject(json);
                    var answer = data?.choices?[0]?.message?.content?.ToString();

                    if (!string.IsNullOrEmpty(answer))
                        return answer;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Groq API Error: {ex.Message}");
            }

            // Fallback responses
            var question = userQuestion.ToLower();

            if (question.Contains("hello") || question.Contains("hi"))
                return "Hello! I'm your AI travel assistant. How can I help plan your trip to Pakistan?";

            if (question.Contains("help"))
                return "I can help you with:\n- Finding best destinations\n- Budget planning\n- Hotel recommendations\n- Transport options\n- Weather information\n- Tour packages\n\nJust ask me anything about travel in Pakistan!";

            if (question.Contains("thank"))
                return "You're welcome! Happy travels! Feel free to ask if you need anything else.";

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

            return "I'm your AI travel assistant! I can help you with destination recommendations, budget planning, hotel bookings, and transport options. What would you like to know about traveling in Pakistan?";
        }

        private string GetFaqAnswer(string question)
        {
            var lowerQuestion = question.ToLower();

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
                if (lowerQuestion.Contains(faq.Key))
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

    public class BudgetComparison
    {
        public string Destination1 { get; set; } = string.Empty;
        public string Destination2 { get; set; } = string.Empty;
        public decimal Cost1 { get; set; }
        public decimal Cost2 { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public decimal Savings { get; set; }
    }
}