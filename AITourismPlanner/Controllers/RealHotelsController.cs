using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AITourismPlanner.Controllers
{
    public class RealHotelsController : Controller

    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;


        public RealHotelsController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            string city = "Islamabad",
            string checkIn = null,
            string checkOut = null,
            int guests = 1)
        {
            if (string.IsNullOrEmpty(checkIn))
                checkIn = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            if (string.IsNullOrEmpty(checkOut))
                checkOut = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");

            ViewBag.City = city;
            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;
            ViewBag.Guests = guests;

            var hotels = await GetHotelsAsync(city, checkIn, checkOut, guests);
            return View(hotels);
        }

        private async Task<List<HotelResult>> GetHotelsAsync(
            string city, string checkIn, string checkOut, int guests)
        {
            var hotels = new List<HotelResult>();
            var apiKey = "35f85c261fmsh0b3fdef51d3d998p1de5fajsn410ef6c10768";
            var host = "booking-com15.p.rapidapi.com";

            try
            {
                // Step 1 - dest_id lo
                var destRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination?query={city}"),
                    Headers =
                    {
                        { "X-RapidAPI-Key", apiKey },
                        { "X-RapidAPI-Host", host },
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

                // Step 2 - Hotels lo
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
                        { "X-RapidAPI-Key", apiKey },
                        { "X-RapidAPI-Host", host },
                    },
                };

                var hotelResponse = await _httpClient.SendAsync(hotelRequest);
                var hotelJson = await hotelResponse.Content.ReadAsStringAsync();
                dynamic hotelData = JsonConvert.DeserializeObject(hotelJson);

                if (hotelData?.data?.hotels != null)
                {
                    foreach (var item in hotelData.data.hotels)
                    {
                        try
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

                            if (!string.IsNullOrEmpty(name))
                            {
                                hotels.Add(new HotelResult
                                {
                                    Id = item?.property?.id?.ToString(),
                                    Name = name,
                                    Address = item?.property?.wishlistName?.ToString() ?? city,
                                    Rating = rating,
                                    PricePerNight = price,
                                    ImageUrl = imageUrl,
                                    ReviewCount = reviewCount
                                });
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return hotels;
        }
        public async Task<IActionResult> TestDestinations()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(
                    "https://opentripmap-places-v1.p.rapidapi.com/en/places/geoname" +
                    "?name=Islamabad"),
                Headers =
        {
            { "X-RapidAPI-Key", "35f85c261fmsh0b3fdef51d3d998p1g5fajsn410ef6c10768" },
            { "X-RapidAPI-Host", "opentripmap-places-v1.p.rapidapi.com" },
        },
            };

            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
        public async Task<IActionResult> DebugSearch()
        {
            var client = new HttpClient();

            // Step 1 - dest_id lo
            var destRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination?query=Islamabad"),
                Headers =
        {
            { "X-RapidAPI-Key", "35f85c261fmsh0b3fdef51d3d998p1g5fajsn410ef6c10768" },
            { "X-RapidAPI-Host", "booking-com15.p.rapidapi.com" }
        },
            };

            var destResponse = await client.SendAsync(destRequest);
            var destJson = await destResponse.Content.ReadAsStringAsync();

            dynamic destData = Newtonsoft.Json.JsonConvert.DeserializeObject(destJson);

            string destId = null;
            string destType = null;

            if (destData?.data != null)
            {
                foreach (var item in destData.data)
                {
                    destType = item?.dest_type?.ToString();
                    if (destType == "city")
                    {
                        destId = item?.dest_id?.ToString();
                        break;
                    }
                }
            }

            return Content($"DestId: {destId}, DestType: {destType}", "text/plain");
        }
        public async Task<IActionResult> TestPlaces()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(
                    "https://opentripmap-places-v1.p.rapidapi.com/en/places/radius" +
                    "?radius=10000" +
                    "&lon=73.04329" +
                    "&lat=33.72148" +
                    "&kinds=interesting_places" +
                    "&limit=10"),
                Headers =
        {
            { "X-RapidAPI-Key", "35f85c261fmsh0b3fdef51d3d998p1g5fajsn410ef6c10768" },
            { "X-RapidAPI-Host", "opentripmap-places-v1.p.rapidapi.com" },
        },
            };

            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
    }

    public class HotelResult
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal? Rating { get; set; }
        public decimal? PricePerNight { get; set; }
        public string ImageUrl { get; set; }
        public int? ReviewCount { get; set; }
    }


}