using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AITourismPlanner.Services
{
    public interface IRealHotelService
    {
        Task<List<RealHotel>> SearchHotelsAsync(string city, string checkIn, string checkOut, int guests = 1);
    }

    public class RealHotel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal? Rating { get; set; }
        public decimal? PricePerNight { get; set; }
        public string Currency { get; set; }
        public string ImageUrl { get; set; }
        public int? ReviewCount { get; set; }
        public string Stars { get; set; }  
        public double Lat { get; set; }    // 🔽 YEH LINE ADD KARO (optional)
        public double Lon { get; set; }    // 🔽 YEH LINE ADD KARO (optional)
        public string Description { get; set; }
    }

    public class RealHotelService : IRealHotelService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _host;

        public RealHotelService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["RapidAPI:Key"];
            _host = configuration["RapidAPI:BookingHost"];
        }

        public async Task<List<RealHotel>> SearchHotelsAsync(
      string city, string checkIn, string checkOut, int guests = 1)
        {
            var hotels = new List<RealHotel>();

            try
            {
                // Step 1 — dest_id lo
                var destRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination?query={city}"),
                    Headers =
            {
                { "X-RapidAPI-Key", _apiKey },
                { "X-RapidAPI-Host", _host },
            },
                };

                var destResponse = await _httpClient.SendAsync(destRequest);
                var destJson = await destResponse.Content.ReadAsStringAsync();
                dynamic destData = JsonConvert.DeserializeObject(destJson);

                string destId = null;
                if (destData?.data != null)
                {
                    foreach (var item in destData.data)
                    {
                        if (item?.dest_type?.ToString() == "city")
                        {
                            destId = item?.dest_id?.ToString();
                            break;

                        }

                    }
                }

                if (destId == null) return hotels;

                // Step 2 — Hotels lo
                var hotelRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(
                        $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchHotels" +
                        $"?dest_id={destId}" +
                        $"&search_type=city" +
                        $"&arrival_date={checkIn}" +
                        $"&departure_date={checkOut}" +
                        $"&adults={guests}" +
                        $"&room_qty=1" +
                        $"&page_number=1" +
                        $"&languagecode=en-us" +
                        $"&currency_code=USD"),
                    Headers =
            {
                { "X-RapidAPI-Key", _apiKey },
                { "X-RapidAPI-Host", _host },
            },
                };

                var hotelResponse = await _httpClient.SendAsync(hotelRequest);
                var hotelJson = await hotelResponse.Content.ReadAsStringAsync();
                dynamic hotelData = JsonConvert.DeserializeObject(hotelJson);

                if (hotelData?.data?.hotels != null)
                {
                    foreach (var item in hotelData.data.hotels)
                    {
                        decimal? price = null;
                        try
                        {
                            if (item?.property?.priceBreakdown?.grossPrice?.value != null)
                                price = decimal.Parse(
                                    item.property.priceBreakdown.grossPrice.value.ToString()) * 278;

                        }
                        catch { }

                        string imageUrl = null;
                        try
                        {
                            if (item?.property?.photoUrls != null)
                                imageUrl = item.property.photoUrls[0]?.ToString();
                        }
                        catch { }

                        decimal? rating = null;
                        try
                        {
                            if (item?.property?.reviewScore != null)
                                rating = decimal.Parse(
                                    item.property.reviewScore.ToString());
                        }
                        catch { }

                        int? reviewCount = null;
                        try
                        {
                            if (item?.property?.reviewCount != null)
                                reviewCount = int.Parse(
                                    item.property.reviewCount.ToString());
                        }
                        catch { }

                        string name = item?.property?.name?.ToString();
                        string address = item?.property?.wishlistName?.ToString() ?? city;
                        string id = item?.property?.id?.ToString();

                        if (!string.IsNullOrEmpty(name))
                        {
                            hotels.Add(new RealHotel
                            {
                                Id = id,
                                Name = name,
                                Address = address,
                                Rating = rating,
                                PricePerNight = price,
                                Currency = "PKR",
                                ImageUrl = imageUrl,
                                ReviewCount = reviewCount
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
            }

            return hotels;
        }
    }
}