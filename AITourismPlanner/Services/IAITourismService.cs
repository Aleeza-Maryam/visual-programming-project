using AITourismPlanner.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AITourismPlanner.Services
{
    public class BudgetComparison
    {
        public string Destination1 { get; set; } = string.Empty;
        public string Destination2 { get; set; } = string.Empty;
        public decimal Cost1 { get; set; }
        public decimal Cost2 { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public decimal Savings { get; set; }
    }

    public interface IAITourismService
    {
        Task<List<Package>> SmartSearch(string query);
        Task<string> AnalyzeReviewSentiment(string reviewText);
        Task<BudgetComparison> CompareBudget(string dest1, string dest2, int days);
        Task<List<Package>> PersonalizedRecommendations(int userId);
        Task<string> GenerateItinerary(string destination, int days, string budget);
        Task<string> ChatbotResponse(string userQuestion, int? userId = null);
    }
}