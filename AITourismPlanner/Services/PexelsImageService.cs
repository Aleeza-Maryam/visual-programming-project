using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AITourismPlanner.Services
{
    public class PexelsImageService : IPexelsImageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public PexelsImageService(IConfiguration configuration)
        {
            _apiKey = configuration["Pexels:ApiKey"] ?? "YOUR_PEXELS_API_KEY";
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
        }

        public async Task<string> GetImageForDestinationAsync(string destinationName)
        {
            try
            {
                var query = Uri.EscapeDataString($"{destinationName} Pakistan landscape");
                var response = await _httpClient.GetAsync(
                    $"https://api.pexels.com/v1/search?query={query}&per_page=1");

                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);
                var photos = data["photos"] as JArray;

                if (photos != null && photos.Count > 0)
                {
                    return photos[0]["src"]["medium"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pexels error: {ex.Message}");
            }

            // Fallback to Unsplash Source
            return $"https://source.unsplash.com/featured/600x400?{destinationName}+Pakistan";
        }

        public async Task<List<string>> GetMultipleImagesAsync(string destinationName, int count = 5)
        {
            var images = new List<string>();
            try
            {
                var query = Uri.EscapeDataString($"{destinationName} Pakistan");
                var response = await _httpClient.GetAsync(
                    $"https://api.pexels.com/v1/search?query={query}&per_page={count}");

                var json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);
                var photos = data["photos"] as JArray;

                if (photos != null)
                {
                    foreach (var photo in photos)
                    {
                        var url = photo["src"]["medium"]?.ToString();
                        if (!string.IsNullOrEmpty(url))
                            images.Add(url);
                    }
                }
            }
            catch { }

            return images;
        }
    }
}