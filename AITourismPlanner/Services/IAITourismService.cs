using AITourismPlanner.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AITourismPlanner.Services
{
    public interface IAITourismService
    {
        Task<List<Package>> SmartSearch(string query);           // Feature 1
        Task<string> AnalyzeReviewSentiment(string reviewText);  // Feature 2
        Task<BudgetComparison> CompareBudget(string dest1, string dest2, int days); // Feature 3
        Task<List<Package>> PersonalizedRecommendations(int userId); // Feature 4
        Task<string> GenerateItinerary(string destination, int days, string budget); // Feature 5
        Task<string> ChatbotResponse(string userQuestion);       // Feature 6
    }

   
}