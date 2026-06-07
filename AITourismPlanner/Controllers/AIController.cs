using Microsoft.AspNetCore.Mvc;
using AITourismPlanner.Services;
using System.Threading.Tasks;

namespace AITourismPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly IAITourismService _aiService;

        public AIController(IAITourismService aiService)
        {
            _aiService = aiService;
        }

        [HttpGet("smart-search")]
        public async Task<IActionResult> SmartSearch(string q)
        {
            var results = await _aiService.SmartSearch(q);
            return Ok(new { query = q, count = results.Count, results });
        }

        [HttpPost("analyze-sentiment")]
        public async Task<IActionResult> AnalyzeSentiment([FromBody] string review)
        {
            var sentiment = await _aiService.AnalyzeReviewSentiment(review);
            return Ok(new { review, sentiment });
        }

        [HttpGet("compare-budget")]
        public async Task<IActionResult> CompareBudget(string dest1, string dest2, int days = 3)
        {
            var comparison = await _aiService.CompareBudget(dest1, dest2, days);
            return Ok(comparison);
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> Recommendations()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized(new { error = "Please login first" });

            var recommendations = await _aiService.PersonalizedRecommendations(userId.Value);
            return Ok(new { count = recommendations.Count, recommendations });
        }

        [HttpPost("generate-itinerary")]
        public async Task<IActionResult> GenerateItinerary([FromBody] ItineraryRequest request)
        {
            var itinerary = await _aiService.GenerateItinerary(request.Destination, request.Days, request.Budget);
            return Ok(new { itinerary });
        }

        // =========================================================
        // PERSONALIZED CHATBOT - Passes userId to service
        // =========================================================
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] string question)
        {
            if (string.IsNullOrEmpty(question))
                return BadRequest(new { error = "Question is required" });

            var userId = HttpContext.Session.GetInt32("UserId");
            var answer = await _aiService.ChatbotResponse(question, userId);
            return Ok(new { question, answer });
        }
    }

    public class ItineraryRequest
    {
        public string Destination { get; set; } = string.Empty;
        public int Days { get; set; } = 3;
        public string Budget { get; set; } = "Medium";
    }
}