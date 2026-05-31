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

        // Pakistan tourist cities with Unsplash images
        private readonly List<(string Name, string Image, string Description)> _pakistanCities =
            new()
    {
        ("Hunza", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "One of the most beautiful valleys in the world, surrounded by majestic mountains."),
        ("Murree", "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&q=80", "Popular hill station near Islamabad, known for cool weather and scenic views."),
        ("Skardu", "https://images.unsplash.com/photo-1580502304784-8985b7eb7260?w=600&q=80", "Gateway to K2 and some of the world's highest peaks."),
        ("Lahore", "https://images.unsplash.com/photo-1599030179987-a7d0ce01bd23?w=600&q=80", "Cultural capital of Pakistan, famous for Mughal architecture and food."),
        ("Islamabad", "https://images.unsplash.com/photo-1609700660014-c4c68e81e84b?w=600&q=80", "The modern capital city surrounded by Margalla Hills."),
        ("Naran", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Famous for Saif-ul-Malook Lake and stunning mountain scenery."),
        ("Swat", "https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80", "Known as the Switzerland of Pakistan with lush green valleys."),
        ("Quetta", "https://images.unsplash.com/photo-1567596275753-92607c3ce1ae?w=600&q=80", "Capital of Balochistan, known for fruits and scenic beauty."),
        ("Karachi", "https://images.unsplash.com/photo-1567157577867-05ccb1388e66?w=600&q=80", "Pakistan largest city and economic hub on the Arabian Sea coast."),
        ("Peshawar", "https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=600&q=80", "Ancient city with rich history, known for its bazaars and culture."),
        ("Gilgit", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Hub of northern Pakistan, surrounded by mighty Karakoram mountains."),
        ("Chitral", "https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80", "Remote valley known for Kalash people and Hindu Kush mountains."),
        ("Abbottabad", "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&q=80", "Beautiful hill city known for Karakoram Highway and scenic beauty."),
        ("Multan", "https://images.unsplash.com/photo-1599030179987-a7d0ce01bd23?w=600&q=80", "City of Saints, known for Sufi shrines and mango orchards."),
        ("Gwadar", "https://images.unsplash.com/photo-1567157577867-05ccb1388e66?w=600&q=80", "Deep sea port city on the Arabian Sea, rising economic hub."),
        ("Ziarat", "https://images.unsplash.com/photo-1567596275753-92607c3ce1ae?w=600&q=80", "Famous for world second largest juniper forest and cool climate."),
        ("Kalash Valley", "https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80", "Unique valley with ancient Kalash culture and colorful festivals."),
        ("Neelum Valley", "https://images.unsplash.com/photo-1580502304784-8985b7eb7260?w=600&q=80", "Scenic valley in AJK famous for its blue river and lush forests."),
        ("Muzaffarabad", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Capital of Azad Kashmir, surrounded by mountains and rivers."),
        ("Fairy Meadows", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Base camp for Nanga Parbat, one of the most scenic spots in Pakistan."),
        ("Khunjerab Pass", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Highest paved international border crossing in the world."),
        ("Attabad Lake", "https://images.unsplash.com/photo-1580502304784-8985b7eb7260?w=600&q=80", "Stunning turquoise lake formed by a landslide in Hunza valley."),
        ("Rawalakot", "https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80", "Pearl Valley of Azad Kashmir with breathtaking landscapes."),
        ("Shogran", "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&q=80", "Beautiful plateau in Kaghan Valley with pine forests."),
        ("Deosai Plains", "https://images.unsplash.com/photo-1580502304784-8985b7eb7260?w=600&q=80", "One of the highest plateaus in the world, habitat of brown bears."),
        ("Bahawalpur", "https://images.unsplash.com/photo-1599030179987-a7d0ce01bd23?w=600&q=80", "City of palaces and gateway to Cholistan Desert."),
        ("Cholistan Desert", "https://images.unsplash.com/photo-1567596275753-92607c3ce1ae?w=600&q=80", "Vast desert with ancient forts and camel safaris."),
        ("Mohenjo Daro", "https://images.unsplash.com/photo-1599030179987-a7d0ce01bd23?w=600&q=80", "UNESCO World Heritage Site, one of oldest civilizations on earth."),
        ("Taxila", "https://images.unsplash.com/photo-1609700660014-c4c68e81e84b?w=600&q=80", "Ancient city and UNESCO site, center of Gandhara civilization."),
        ("Rohtas Fort", "https://images.unsplash.com/photo-1599030179987-a7d0ce01bd23?w=600&q=80", "UNESCO World Heritage Fort built by Sher Shah Suri."),
        ("Makran Coast", "https://images.unsplash.com/photo-1567157577867-05ccb1388e66?w=600&q=80", "Stunning coastline along the Arabian Sea with unique landscapes."),
        ("Hingol National Park", "https://images.unsplash.com/photo-1567596275753-92607c3ce1ae?w=600&q=80", "Largest national park in Pakistan with diverse wildlife."),
        ("Khyber Pass", "https://images.unsplash.com/photo-1558618047-3c8c76ca7d13?w=600&q=80", "Historic mountain pass connecting Pakistan and Afghanistan."),
        ("Thar Desert", "https://images.unsplash.com/photo-1567596275753-92607c3ce1ae?w=600&q=80", "Colorful desert of Sindh with unique culture and sand dunes."),
        ("Kalam", "https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80", "Scenic valley in upper Swat surrounded by lush forests."),
        ("Malam Jabba", "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&q=80", "Pakistan only ski resort in Swat valley."),
        ("Ratti Gali Lake", "https://images.unsplash.com/photo-1580502304784-8985b7eb7260?w=600&q=80", "Alpine lake in AJK, one of the most beautiful lakes in Pakistan."),
        ("Shangrila Resort", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Heaven on Earth near Skardu with stunning lake views."),
        ("Khaplu", "https://images.unsplash.com/photo-1580502304784-8985b7eb7260?w=600&q=80", "Ancient town in Baltistan with historic palace and glaciers."),
        ("Passu", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Famous for Passu Cones and stunning Karakoram scenery."),
        ("Naltar Valley", "https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=600&q=80", "Famous for colorful lakes and Pakistan Air Force ski resort."),
        ("Phander Valley", "https://images.unsplash.com/photo-1580502304784-8985b7eb7260?w=600&q=80", "Hidden gem in Gilgit Baltistan with stunning lake and scenery."),
        ("Ghanche", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Remote district near K2 base camp in Baltistan."),
        ("Shigar Valley", "https://images.unsplash.com/photo-1580502304784-8985b7eb7260?w=600&q=80", "Historic valley with ancient fort and stunning mountain views."),
        ("Gorakh Hill", "https://images.unsplash.com/photo-1567596275753-92607c3ce1ae?w=600&q=80", "Balochistan highest peak with cool climate and scenic views."),
        ("Hanna Lake", "https://images.unsplash.com/photo-1567596275753-92607c3ce1ae?w=600&q=80", "Beautiful lake near Quetta, popular picnic spot."),
        ("Mansehra", "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600&q=80", "Gateway to Kaghan Valley with ancient Ashoka inscriptions."),
        ("Besham", "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80", "Town on Karakoram Highway along the Indus River."),
        ("Faisalabad", "https://images.unsplash.com/photo-1599030179987-a7d0ce01bd23?w=600&q=80", "Industrial city known as Manchester of Pakistan."),
        ("Hyderabad", "https://images.unsplash.com/photo-1567157577867-05ccb1388e66?w=600&q=80", "Historical city of Sindh with ancient forts and culture."),
        ("Sukkur", "https://images.unsplash.com/photo-1567157577867-05ccb1388e66?w=600&q=80", "City on Indus River with ancient Sukkur Barrage."),
    };
        public DestinationApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent", "AITourismPlanner/1.0");
        }

        public async Task<List<DestinationInfo>> GetPakistanDestinationsAsync(
            string search = "")
        {
            var results = new List<DestinationInfo>();

            var cities = string.IsNullOrEmpty(search)
                ? _pakistanCities
                : _pakistanCities.Where(c =>
                    c.Name.ToLower().Contains(search.ToLower())).ToList();

            // Agar search mein koi nahi mila
            if (!cities.Any() && !string.IsNullOrEmpty(search))
            {
                cities = new List<(string, string, string)>
                {
                    (search,
                     "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80",
                     $"Explore the beautiful city of {search} in Pakistan.")
                };
            }

            // Pehle 12 cities ke liye coordinates lo
            foreach (var city in cities.Take(50))
            {
                try
                {
                    var response = await _httpClient.GetAsync(
                        $"https://nominatim.openstreetmap.org/search" +
                        $"?q={Uri.EscapeDataString(city.Name + " Pakistan")}" +
                        $"&format=json&limit=1&addressdetails=1");

                    var json = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(json);

                    if (data != null && data.Count > 0)
                    {
                        results.Add(new DestinationInfo
                        {
                            Name = city.Name,
                            DisplayName = city.Name + ", Pakistan",
                            Country = "Pakistan",
                            Lat = double.Parse(data[0].lat.ToString()),
                            Lon = double.Parse(data[0].lon.ToString()),
                            ImageUrl = city.Image,
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
                            ImageUrl = city.Image,
                            Description = city.Description
                        });
                    }

                    // Rate limit se bachne ke liye
                    await Task.Delay(200);
                }
                catch
                {
                    results.Add(new DestinationInfo
                    {
                        Name = city.Name,
                        DisplayName = city.Name + ", Pakistan",
                        Country = "Pakistan",
                        ImageUrl = city.Image,
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
                    .FirstOrDefault(c =>
                        c.Name.ToLower() == cityName.ToLower());

                string imageUrl = cityData.Image ??
                    "https://images.unsplash.com/photo-1586500036706-41963de24d8b?w=600&q=80";
                string description = cityData.Description ??
                    $"Explore the beautiful {cityName} in Pakistan.";

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