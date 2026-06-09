using Newtonsoft.Json;

namespace AITourismPlanner.Services
{
    public class DestinationInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Country { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }

        public List<NearbyPlace> Attractions { get; set; } = new();
    }

    public class NearbyPlace
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public double Distance { get; set; }
        public string ImageUrl { get; set; }
        
    }

    public interface IDestinationApiService
    {
        Task<DestinationInfo> GetDestinationInfoAsync(string cityName);
        Task<List<DestinationInfo>> GetPakistanDestinationsAsync(string search = "");
    }

    public class DestinationApiService : IDestinationApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IPexelsImageService _pexelsService;

        // Pakistan tourist cities (Name + Description only, images come from Pexels)
        private readonly List<(string Name, string Description)> _pakistanCities = new()
        {
            ("Hunza", "One of the most beautiful valleys in the world, surrounded by majestic mountains."),
            ("Murree", "Popular hill station near Islamabad, known for cool weather and scenic views."),
            ("Skardu", "Gateway to K2 and some of the world's highest peaks."),
            ("Lahore", "Cultural capital of Pakistan, famous for Mughal architecture and food."),
            ("Islamabad", "The modern capital city surrounded by Margalla Hills."),
            ("Naran", "Famous for Saif-ul-Malook Lake and stunning mountain scenery."),
            ("Swat", "Known as the Switzerland of Pakistan with lush green valleys."),
            ("Quetta", "Capital of Balochistan, known for fruits and scenic beauty."),
            ("Karachi", "Pakistan's largest city and economic hub on the Arabian Sea coast."),
            ("Peshawar", "Ancient city with rich history, known for its bazaars and culture."),
            ("Gilgit", "Hub of northern Pakistan, surrounded by mighty Karakoram mountains."),
            ("Chitral", "Remote valley known for Kalash people and Hindu Kush mountains."),
            ("Abbottabad", "Beautiful hill city known for Karakoram Highway and scenic beauty."),
            ("Multan", "City of Saints, known for Sufi shrines and mango orchards."),
            ("Gwadar", "Deep sea port city on the Arabian Sea, rising economic hub."),
            ("Ziarat", "Famous for world's second largest juniper forest and cool climate."),
            ("Kalash Valley", "Unique valley with ancient Kalash culture and colorful festivals."),
            ("Neelum Valley", "Scenic valley in AJK famous for its blue river and lush forests."),
            ("Muzaffarabad", "Capital of Azad Kashmir, surrounded by mountains and rivers."),
            ("Fairy Meadows", "Base camp for Nanga Parbat, one of the most scenic spots in Pakistan."),
            ("Khunjerab Pass", "Highest paved international border crossing in the world."),
            ("Attabad Lake", "Stunning turquoise lake formed by a landslide in Hunza valley."),
            ("Rawalakot", "Pearl Valley of Azad Kashmir with breathtaking landscapes."),
            ("Shogran", "Beautiful plateau in Kaghan Valley with pine forests."),
            ("Deosai Plains", "One of the highest plateaus in the world, habitat of brown bears."),
            ("Bahawalpur", "City of palaces and gateway to Cholistan Desert."),
            ("Cholistan Desert", "Vast desert with ancient forts and camel safaris."),
            ("Mohenjo Daro", "UNESCO World Heritage Site, one of the oldest civilizations on earth."),
            ("Taxila", "Ancient city and UNESCO site, center of Gandhara civilization."),
            ("Rohtas Fort", "UNESCO World Heritage Fort built by Sher Shah Suri."),
            ("Makran Coast", "Stunning coastline along the Arabian Sea with unique landscapes."),
            ("Hingol National Park", "Largest national park in Pakistan with diverse wildlife."),
            ("Khyber Pass", "Historic mountain pass connecting Pakistan and Afghanistan."),
            ("Thar Desert", "Colorful desert of Sindh with unique culture and sand dunes."),
            ("Kalam", "Scenic valley in upper Swat surrounded by lush forests."),
            ("Malam Jabba", "Pakistan's only ski resort in Swat valley."),
            ("Ratti Gali Lake", "Alpine lake in AJK, one of the most beautiful lakes in Pakistan."),
            ("Shangrila Resort", "Heaven on Earth near Skardu with stunning lake views."),
            ("Khaplu", "Ancient town in Baltistan with historic palace and glaciers."),
            ("Passu", "Famous for Passu Cones and stunning Karakoram scenery."),
            ("Naltar Valley", "Famous for colorful lakes and Pakistan Air Force ski resort."),
            ("Phander Valley", "Hidden gem in Gilgit Baltistan with stunning lake and scenery."),
            ("Ghanche", "Remote district near K2 base camp in Baltistan."),
            ("Shigar Valley", "Historic valley with ancient fort and stunning mountain views."),
            ("Gorakh Hill", "Balochistan's highest peak with cool climate and scenic views."),
            ("Hanna Lake", "Beautiful lake near Quetta, popular picnic spot."),
            ("Mansehra", "Gateway to Kaghan Valley with ancient Ashoka inscriptions."),
            ("Besham", "Town on Karakoram Highway along the Indus River."),
            ("Faisalabad", "Industrial city known as Manchester of Pakistan."),
            ("Hyderabad", "Historical city of Sindh with ancient forts and culture."),
            ("Sukkur", "City on Indus River with ancient Sukkur Barrage.")
        };

        public DestinationApiService(HttpClient httpClient, IPexelsImageService pexelsService)
        {
            _httpClient = httpClient;
            _pexelsService = pexelsService;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AITourismPlanner/1.0");
        }

        // 🔥 NEW: Async method to get image URL from Pexels
        private async Task<string> GetRelevantImageUrlAsync(string cityName)
        {
            try
            {
                return await _pexelsService.GetImageForDestinationAsync(cityName);
            }
            catch
            {
                // Fallback to Unsplash Source if Pexels fails
                return $"https://source.unsplash.com/featured/600x400?{Uri.EscapeDataString(cityName + " Pakistan")}";
            }
        }

        public async Task<List<DestinationInfo>> GetPakistanDestinationsAsync(string search = "")
        {
            var results = new List<DestinationInfo>();

            var cities = string.IsNullOrEmpty(search)
                ? _pakistanCities
                : _pakistanCities.Where(c => c.Name.ToLower().Contains(search.ToLower())).ToList();

            if (!cities.Any() && !string.IsNullOrEmpty(search))
            {
                cities = new List<(string, string)>
                {
                    (search, $"Explore the beautiful city of {search} in Pakistan.")
                };
            }

            foreach (var city in cities.Take(50))
            {
                try
                {
                    // Get coordinates from Nominatim (OpenStreetMap)
                    var response = await _httpClient.GetAsync(
                        $"https://nominatim.openstreetmap.org/search" +
                        $"?q={Uri.EscapeDataString(city.Name + " Pakistan")}" +
                        $"&format=json&limit=1&addressdetails=1");

                    var json = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(json);

                    // ✅ Get image from Pexels (async)
                    string imageUrl = await GetRelevantImageUrlAsync(city.Name);

                    if (data != null && data.Count > 0)
                    {
                        results.Add(new DestinationInfo
                        {
                            Name = city.Name,
                            DisplayName = city.Name + ", Pakistan",
                            Country = "Pakistan",
                            Lat = double.Parse(data[0].lat.ToString()),
                            Lon = double.Parse(data[0].lon.ToString()),
                            ImageUrl = imageUrl,
                            Description = city.Description
                        });
                    }
                    else
                    {
                        results.Add(new DestinationInfo
                        {
                            Name = city.Name,
                            DisplayName = city.Name + ", Pakistan",
                            Country = "Pakistan",
                            ImageUrl = imageUrl,
                            Description = city.Description
                        });
                    }

                    // Rate limit for Nominatim (1 request per second)
                    await Task.Delay(200);
                }
                catch (Exception ex)
                {
                    // Fallback on error
                    results.Add(new DestinationInfo
                    {
                        Name = city.Name,
                        DisplayName = city.Name + ", Pakistan",
                        Country = "Pakistan",
                        ImageUrl = await GetRelevantImageUrlAsync(city.Name),
                        Description = city.Description
                    });
                }
            }

            return results;
        }

        public async Task<DestinationInfo> GetDestinationInfoAsync(string cityName)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"https://nominatim.openstreetmap.org/search" +
                    $"?q={Uri.EscapeDataString(cityName + " Pakistan")}" +
                    $"&format=json&limit=1&addressdetails=1");

                var json = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(json);

                var cityData = _pakistanCities
                    .FirstOrDefault(c => c.Name.ToLower() == cityName.ToLower());

                string description = cityData.Description ??
                    $"Explore the beautiful {cityName} in Pakistan.";

                // ✅ Get image from Pexels (async)
                string imageUrl = await GetRelevantImageUrlAsync(cityName);

                if (data != null && data.Count > 0)
                {
                    return new DestinationInfo
                    {
                        Name = cityName,
                        DisplayName = data[0].display_name?.ToString() ?? cityName,
                        Country = "Pakistan",
                        Lat = double.Parse(data[0].lat.ToString()),
                        Lon = double.Parse(data[0].lon.ToString()),
                        ImageUrl = imageUrl,
                        Description = description
                    };
                }

                return new DestinationInfo
                {
                    Name = cityName,
                    DisplayName = cityName + ", Pakistan",
                    Country = "Pakistan",
                    ImageUrl = imageUrl,
                    Description = description
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
    }
}