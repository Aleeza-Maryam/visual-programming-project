using AITourismPlanner.Data;
using AITourismPlanner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
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
        // SMART SEARCH
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
        // SENTIMENT ANALYSIS
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
        // BUDGET COMPARISON
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
        // PERSONALIZED RECOMMENDATIONS
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
        // ITINERARY GENERATOR
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
        // FULLY PERSONALIZED AI CHATBOT
        // =========================================================
        public async Task<string> ChatbotResponse(string userQuestion, int? userId = null)
        {
            if (string.IsNullOrEmpty(userQuestion))
                return "Please ask me something about travel!";

            var question = userQuestion.ToLower();

            // Get user data if logged in
            User? currentUser = null;
            List<PackageBooking> userBookings = new List<PackageBooking>();
            List<Wishlist> userWishlist = new List<Wishlist>();

            if (userId.HasValue)
            {
                currentUser = await _context.users.FirstOrDefaultAsync(u => u.user_id == userId);
                userBookings = await _context.package_bookings
                    .Where(b => b.user_id == userId)
                    .Include(b => b.Package)
                    .OrderByDescending(b => b.booking_date)
                    .ToListAsync();

                userWishlist = await _context.wishlists
                    .Where(w => w.user_id == userId)
                    .ToListAsync();
            }

            // =========================================================
            // PERSONALIZED RESPONSES
            // =========================================================

            // 1. Greeting with user's name
            if (question.Contains("hello") || question.Contains("hi") || question.Contains("assalam"))
            {
                if (currentUser != null)
                {
                    return $"Assalam-o-Alaikum, {currentUser.full_name}! 👋\n\nI'm your personal AI travel assistant. I see you've been exploring our packages. How can I help you plan your next adventure today?";
                }
                return "Assalam-o-Alaikum! 👋 I'm your AI travel assistant. Please login to get personalized recommendations!";
            }

            // 2. My Bookings / My Trips
            if (question.Contains("my booking") || question.Contains("my trip") || question.Contains("my travels"))
            {
                if (userBookings.Any())
                {
                    var response = $"📋 **Your Bookings** ({userBookings.Count} total)\n\n";
                    foreach (var booking in userBookings.Take(5))
                    {
                        response += $"✈️ **{booking.Package?.package_name}**\n";
                        response += $"   📍 {booking.Package?.destination_name}\n";
                        response += $"   📅 {booking.travel_date:dd MMM yyyy}\n";
                        response += $"   💰 PKR {booking.final_price:N0}\n";
                        response += $"   Status: {booking.booking_status}\n\n";
                    }
                    if (userBookings.Count > 5)
                        response += $"And {userBookings.Count - 5} more bookings...\n";
                    response += "\nWant to book another trip? Just say 'book a package'!";
                    return response;
                }
                return "You don't have any bookings yet. 📭\n\nWould you like me to recommend some amazing packages for you? Just say 'recommend me something'!";
            }

            // 3. My Wishlist / Favorites
            if (question.Contains("wishlist") || question.Contains("favorite") || question.Contains("saved"))
            {
                if (userWishlist.Any())
                {
                    var response = $"❤️ **Your Wishlist** ({userWishlist.Count} destinations)\n\n";
                    foreach (var item in userWishlist.Take(5))
                    {
                        response += $"📍 {item.destination_name}\n";
                    }
                    response += "\nWant to book any of these? Just say 'book [destination name]'!";
                    return response;
                }
                return "Your wishlist is empty. 😢\n\nStart exploring destinations and click the heart icon to save your favorites!";
            }

            // 4. Recommendations based on past bookings
            if (question.Contains("recommend") || question.Contains("suggest") || question.Contains("what should i book"))
            {
                if (userBookings.Any())
                {
                    var lastPackage = userBookings.First().Package;
                    var preferredDest = lastPackage?.destination_name ?? "Hunza";
                    var preferredType = lastPackage?.package_type ?? "Standard";

                    var recommendations = await _context.packages
                        .Where(p => p.destination_name != preferredDest && p.package_type == preferredType && p.is_active)
                        .Take(3)
                        .ToListAsync();

                    if (recommendations.Any())
                    {
                        var response = $"🎯 **Based on your previous trip to {preferredDest}**\n\n";
                        response += $"I recommend these {preferredType} packages:\n";
                        foreach (var rec in recommendations)
                        {
                            response += $"• **{rec.package_name}** - PKR {rec.price_per_person:N0} ({rec.duration_days} days)\n";
                        }
                        response += "\nWould you like details about any of these?";
                        return response;
                    }
                }

                // Generic recommendation
                var topPackages = await _context.packages
                    .Where(p => p.is_active)
                    .OrderByDescending(p => p.price_per_person)
                    .Take(3)
                    .ToListAsync();

                var genResponse = "🌟 **Top Recommended Packages** 🌟\n\n";
                foreach (var pkg in topPackages)
                {
                    genResponse += $"✨ {pkg.package_name}\n";
                    genResponse += $"   📍 {pkg.destination_name} | 🏨 {pkg.hotel_stars}⭐\n";
                    genResponse += $"   💰 PKR {pkg.price_per_person:N0} | 📅 {pkg.duration_days} days\n\n";
                }
                genResponse += "Which one interests you? I can tell you more!";
                return genResponse;
            }

            // 5. Budget-based with personal spending history
            if (question.Contains("budget"))
            {
                var words = question.Split(' ');
                foreach (var word in words)
                {
                    if (int.TryParse(word, out int budget))
                    {
                        // Get user's average spending if exists
                        decimal avgSpending = 0;
                        if (userBookings.Any())
                        {
                            avgSpending = userBookings.Average(b => b.final_price);
                        }

                        var packages = await _context.packages
                            .Where(p => p.price_per_person <= budget && p.is_active)
                            .Take(3)
                            .ToListAsync();

                        if (packages.Any())
                        {
                            var response = $"💰 With PKR {budget:N0} budget";
                            if (avgSpending > 0)
                            {
                                response += $" (similar to your previous trips averaging PKR {avgSpending:N0})";
                            }
                            response += ", you can consider:\n\n";
                            foreach (var pkg in packages)
                            {
                                response += $"• **{pkg.package_name}** - PKR {pkg.price_per_person:N0}\n";
                                response += $"  📍 {pkg.destination_name} | {pkg.duration_days} days\n\n";
                            }
                            return response;
                        }
                        return $"No packages found under PKR {budget:N0}. Try increasing your budget to PKR {budget + 10000:N0}!";
                    }
                }
                return "Please tell me your budget amount (e.g., 'budget 50000') and I'll find packages for you!";
            }

            // 6. Destination details with personal connection
            var destinations = new[] { "hunza", "murree", "skardu", "naran", "swat", "lahore", "islamabad", "gilgit" };
            foreach (var dest in destinations)
            {
                if (question.Contains(dest))
                {
                    var hasVisited = userBookings.Any(b => b.Package != null && b.Package.destination_name.ToLower().Contains(dest));

                    var destInfo = GetDestinationInfo(dest);
                    if (hasVisited)
                    {
                        return $"📍 **{dest.TitleCase()}** - You've been here before! 🎉\n\n{destInfo}\n\nWant to explore a new destination this time? Try {GetAlternativeDestination(dest)}!";
                    }
                    return $"📍 **{dest.TitleCase()}**\n\n{destInfo}\n\nWould you like me to show you available packages for {dest.TitleCase()}?";
                }
            }

            // 7. Check if user asked about their profile
            if (question.Contains("my name") || question.Contains("who am i"))
            {
                if (currentUser != null)
                {
                    return $"Your name is {currentUser.full_name}. You registered with email {currentUser.email} on {currentUser.created_at:dd MMM yyyy}.";
                }
                return "You are not logged in. Please login first!";
            }

            // 8. Check packages status
            if (question.Contains("how many packages") || question.Contains("total packages"))
            {
                var totalPackages = await _context.packages.CountAsync(p => p.is_active);
                var packagesByType = await _context.packages
                    .Where(p => p.is_active)
                    .GroupBy(p => p.package_type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                var response = $"📦 We have **{totalPackages}** tour packages available!\n\n";
                foreach (var pt in packagesByType)
                {
                    response += $"• {pt.Type}: {pt.Count} packages\n";
                }
                return response;
            }

            // 9. Fallback to Groq AI (if API key exists)
            if (!string.IsNullOrEmpty(_groqApiKey) && _groqApiKey != "gsk_YOUR_API_KEY_HERE")
            {
                try
                {
                    var userContext = "";
                    if (currentUser != null)
                    {
                        userContext = $"The user's name is {currentUser.full_name}. ";
                        if (userBookings.Any())
                        {
                            userContext += $"They have booked {userBookings.Count} trips before. ";
                            var lastDest = userBookings.First().Package?.destination_name;
                            if (lastDest != null)
                                userContext += $"Their last trip was to {lastDest}. ";
                        }
                    }

                    var requestBody = new
                    {
                        model = _groqModel,
                        messages = new[]
                        {
                            new {
                                role = "system",
                                content = $"You are a friendly Pakistan travel assistant. {userContext}Keep responses concise (2-3 sentences), helpful, and personalized. Recommend packages from Hunza, Murree, Skardu, Naran, Swat, Gilgit."
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Groq API Error: {ex.Message}");
                }
            }

            // 10. Default response
            return "I'm your personal travel assistant! 🤖\n\nI can help you with:\n• Finding destinations\n• Budget planning\n• Checking your bookings\n• Recommending packages\n• Answering travel questions\n\nWhat would you like to know?";
        }

        // Helper methods
        private string GetDestinationInfo(string dest)
        {
            var info = new Dictionary<string, string>
            {
                { "hunza", "Hunza Valley is famous for its stunning mountain views, ancient forts (Baltit & Altit), and friendly locals. Best time: May-October. Estimated cost: PKR 50,000-85,000 for 4-6 days." },
                { "murree", "Murree is a popular hill station with pine forests and colonial architecture. Best time: March-October (Dec-Feb for snow). Estimated cost: PKR 8,000-25,000 for 2-3 days." },
                { "skardu", "Skardu is the gateway to K2, featuring Shangrila Lake, Cold Desert, and Deosai Plains. Best time: June-September. Estimated cost: PKR 45,000-120,000 for 5-6 days." },
                { "naran", "Naran Kaghan is known for Lake Saif-ul-Mulook and beautiful valleys. Best time: May-September. Estimated cost: PKR 10,000-18,000 for 3-4 days." },
                { "swat", "Swat Valley is called Switzerland of the East, with Malam Jabba and Buddhist heritage. Best time: April-October. Estimated cost: PKR 25,000-45,000 for 4-5 days." }
            };

            return info.ContainsKey(dest) ? info[dest] : $"{dest.TitleCase()} is a beautiful destination in Pakistan with rich culture and stunning landscapes.";
        }

        private string GetAlternativeDestination(string current)
        {
            var alternatives = new Dictionary<string, string>
            {
                { "hunza", "Skardu or Gilgit" },
                { "murree", "Naran or Swat" },
                { "skardu", "Hunza or Gilgit" },
                { "naran", "Murree or Swat" },
                { "swat", "Naran or Murree" }
            };
            return alternatives.ContainsKey(current) ? alternatives[current] : "Hunza or Skardu";
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

// Extension method for title case
public static class StringExtensions
{
    public static string TitleCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        return char.ToUpper(str[0]) + str.Substring(1);
    }
}