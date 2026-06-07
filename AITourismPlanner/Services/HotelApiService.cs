using Newtonsoft.Json;

namespace AITourismPlanner.Services
{
    public class HotelApiService : IHotelApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _host;

        public HotelApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["RapidAPI:Key"] ?? "";
            _host = "booking-com15.p.rapidapi.com";

            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", _apiKey);
            _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", _host);
        }

        public async Task<List<HotelModel>> GetNearbyHotelsAsync(string city, DateTime checkIn, DateTime checkOut, int guests = 2)
        {
            var hotels = new List<HotelModel>();

            try
            {
                var destId = GetCityCode(city);
                var checkInStr = checkIn.ToString("yyyy-MM-dd");
                var checkOutStr = checkOut.ToString("yyyy-MM-dd");
                var nights = (checkOut - checkIn).Days;

                var url = $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchHotels?dest_id={destId}&search_type=city&arrival_date={checkInStr}&departure_date={checkOutStr}&adults={guests}&room_qty=1&page_number=1&currency_code=PKR";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return GetMockHotels(city);

                var json = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(json);

                if (data?.data?.hotels != null)
                {
                    foreach (var item in data.data.hotels)
                    {
                        var hotel = new HotelModel();

                        if (item?.property?.id != null)
                            hotel.Id = item.property.id.ToString();

                        if (item?.property?.name != null)
                            hotel.Name = item.property.name.ToString();

                        if (item?.property?.wishlistName != null)
                            hotel.Address = item.property.wishlistName.ToString();
                        else
                            hotel.Address = city;

                  
                        if (item?.property?.reviewScore != null)
                        {
                            string ratingStr = item.property.reviewScore.ToString();
                            double ratingValue;
                            if (double.TryParse(ratingStr, out ratingValue))
                            {
                                hotel.Rating = ratingValue;
                            }
                        }

                        if (item?.property?.photoUrls != null && item.property.photoUrls.Count > 0)
                            hotel.ImageUrl = item.property.photoUrls[0]?.ToString();

                        // FIXED: Explicit type declaration instead of out var
                        if (item?.property?.priceBreakdown?.grossPrice?.value != null)
                        {
                            string priceStr = item.property.priceBreakdown.grossPrice.value.ToString();
                            decimal priceUsdValue;
                            if (decimal.TryParse(priceStr, out priceUsdValue))
                            {
                                hotel.PricePerNight = priceUsdValue * 278;
                            }
                        }

                        hotel.BookingLink = $"https://www.booking.com/hotel/{hotel.Id}.html";

                        if (!string.IsNullOrEmpty(hotel.Name))
                            hotels.Add(hotel);
                    }
                }

                if (hotels.Count == 0)
                    return GetMockHotels(city);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hotel API Error: {ex.Message}");
                return GetMockHotels(city);
            }

            return hotels.Take(6).ToList();
        }

        private List<HotelModel> GetMockHotels(string city)
        {
            return new List<HotelModel>
            {
                new HotelModel { Id = "1", Name = "Serena Hotel", Address = city, Rating = 4.8, PricePerNight = 18000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=400", ReviewCount = 450, Amenities = new List<string> { "Free WiFi", "Pool", "Spa", "Restaurant" } },
                new HotelModel { Id = "2", Name = "Pearl Continental", Address = city, Rating = 4.5, PricePerNight = 15000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=400", ReviewCount = 320, Amenities = new List<string> { "Free WiFi", "Restaurant", "Gym", "Business Center" } },
                new HotelModel { Id = "3", Name = "Marriott Hotel", Address = city, Rating = 4.7, PricePerNight = 22000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=400", ReviewCount = 280, Amenities = new List<string> { "Free WiFi", "Pool", "Spa", "Fine Dining" } },
                new HotelModel { Id = "4", Name = "Avari Hotel", Address = city, Rating = 4.3, PricePerNight = 12000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=400", ReviewCount = 190, Amenities = new List<string> { "Free WiFi", "Restaurant", "Parking" } }
            };
        }

        private string GetCityCode(string city)
        {
            return city?.ToLower() switch
            {
                "hunza" => "-900055149",
                "murree" => "-20033875",
                "skardu" => "-900055151",
                "islamabad" => "-221847",
                "lahore" => "-221884",
                "karachi" => "-221876",
                "swat" => "-20033876",
                "naran" => "-20033877",
                _ => "-221847"
            };
        }
    }
}