using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using AITourismPlanner.Services;
using AITourismPlanner.ViewModels;
using Newtonsoft.Json;

namespace AITourismPlanner.Controllers
{
    public class DestinationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDestinationApiService _destinationApi;
        private readonly IWeatherService _weatherService;

        public DestinationsController(
            ApplicationDbContext context,
            IDestinationApiService destinationApi,
            IWeatherService weatherService)
        {
            _context = context;
            _destinationApi = destinationApi;
            _weatherService = weatherService;
        }

        // =========================================================
        // INDEX
        // =========================================================
        public async Task<IActionResult> Index(string searchTerm = "")
        {
            ViewBag.SearchTerm = searchTerm;
            var destinations = await _destinationApi
                .GetPakistanDestinationsAsync(searchTerm);
            return View(destinations);
        }

        // =========================================================
        // API DETAILS
        // =========================================================
        public async Task<IActionResult> ApiDetails(string cityName)
        {
            if (string.IsNullOrEmpty(cityName))
                return RedirectToAction("Index");

            var checkIn = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            var checkOut = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");

            // Parallel API calls
            var destTask = _destinationApi.GetDestinationInfoAsync(cityName);
            var weatherTask = _weatherService.GetCurrentWeatherAsync(cityName);
            var forecastTask = _weatherService.GetWeatherForecastAsync(cityName, 5);
            var hotelsTask = GetHotelsDirectAsync(cityName, checkIn, checkOut);

            // Database se transport aur reviews
            var transports = await _context.transports
                .Where(t => t.arrival_city.ToLower() == cityName.ToLower() ||
                            t.departure_city.ToLower() == cityName.ToLower())
                .ToListAsync();

            var reviews = await _context.destination_reviews
                .Include(r => r.User)
                .Where(r => r.destination_name.ToLower() == cityName.ToLower())
                .OrderByDescending(r => r.created_at)
                .ToListAsync();

            await Task.WhenAll(destTask, weatherTask, forecastTask, hotelsTask);

            double avgRating = reviews.Any()
                ? reviews.Average(r => r.rating ?? 0) : 0;

            var viewModel = new ApiDestinationViewModel
            {
                CityName = cityName,
                DestinationInfo = destTask.Result,
                CurrentWeather = weatherTask.Result,
                Forecast = forecastTask.Result,
                NearbyHotels = hotelsTask.Result,
                Transports = transports,
                Reviews = reviews,
                AverageRating = avgRating,
                TotalReviews = reviews.Count
            };

            return View(viewModel);
        }

        // =========================================================
        // ADD REVIEW
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> AddReview(
            string destinationName, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Please login first" });

            // Check karo pehle review diya hai ya nahi
            var existing = await _context.destination_reviews
                .FirstOrDefaultAsync(r =>
                    r.user_id == userId &&
                    r.destination_name.ToLower() == destinationName.ToLower());

            if (existing != null)
            {
                existing.rating = rating;
                existing.comment = comment;
                existing.created_at = DateTime.Now;
            }
            else
            {
                var review = new DestinationReview
                {
                    user_id = userId.Value,
                    destination_name = destinationName,
                    rating = rating,
                    comment = comment,
                    created_at = DateTime.Now
                };
                _context.destination_reviews.Add(review);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Review submitted!" });
        }

        // =========================================================
        // DELETE REVIEW
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var review = await _context.destination_reviews
                .FindAsync(reviewId);

            if (review != null && review.user_id == userId)
            {
                _context.destination_reviews.Remove(review);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        // ===================================================================
        // BOOKING
        // ===================================================================
        [HttpPost]
        public async Task<IActionResult> CreateBooking(
            string destinationName, string hotelName,
            decimal hotelPricePerNight, int? transportId,
            DateTime checkIn, DateTime checkOut,

            int guests, decimal totalPrice)
        {

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Please login first" });

            var reference = "TRP" + DateTime.Now.ToString("yyyyMMddHHmmss") +
                           new Random().Next(100, 999);

            var booking = new Booking
            {
                user_id = userId.Value,
                destination_name = destinationName,
                hotel_name = hotelName,
                hotel_price_per_night = hotelPricePerNight,
                transport_id = transportId,
                check_in_date = checkIn,
                check_out_date = checkOut,
                number_of_guests = guests,
                total_price = totalPrice,

                booking_status = "Confirmed",
                payment_status = "Pending",
                booking_reference = reference,
                created_at = DateTime.Now
            };


            _context.bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                reference = reference,
                message = "Booking confirmed!"
            });
        }

        // =========================================================
        // HOTELS DIRECT FETCH
        // =========================================================
        // =========================================================
        // HOTELS DIRECT FETCH - HYBRID (API First, Mock Fallback)
        // =========================================================
        private async Task<List<RealHotel>> GetHotelsDirectAsync(
            string city, string checkIn, string checkOut)
        {
            var hotels = new List<RealHotel>();
            var apiKey = "35f85c261fmsh0b3fdef51d3d998p1de5fajsn410ef6c10768";
            var host = "booking-com15.p.rapidapi.com";

            bool apiSuccess = false;

            try
            {
                // Step 1: Try API Call
                var client = new HttpClient();

                var destRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(
                        $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination?query={city}"),
                    Headers =
            {
                { "X-RapidAPI-Key", apiKey },
                { "X-RapidAPI-Host", host },
            },
                };

                var destResponse = await client.SendAsync(destRequest);
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

                if (destId != null)
                {
                    var hotelRequest = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(
                            $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchHotels" +
                            $"?dest_id={destId}" +
                            $"&search_type=city" +
                            $"&arrival_date={checkIn}" +
                            $"&departure_date={checkOut}" +
                            $"&adults=1&room_qty=1&page_number=1" +
                            $"&languagecode=en-us&currency_code=USD"),
                        Headers =
                {
                    { "X-RapidAPI-Key", apiKey },
                    { "X-RapidAPI-Host", host },
                },
                    };

                    var hotelResponse = await client.SendAsync(hotelRequest);
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

                                string name = item?.property?.name?.ToString();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    hotels.Add(new RealHotel
                                    {
                                        Id = item?.property?.id?.ToString(),
                                        Name = name,
                                        Address = item?.property?.wishlistName?.ToString() ?? city,
                                        Rating = rating,
                                        PricePerNight = price,
                                        Currency = "PKR",
                                        ImageUrl = imageUrl,
                                        ReviewCount = item?.property?.reviewCount != null
                                            ? (int?)int.Parse(item.property.reviewCount.ToString())
                                            : null
                                    });
                                    apiSuccess = true;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}. Using mock data.");
            }

            // ✅ If API fails or returns no hotels, use MOCK DATA with proper images
            if (!apiSuccess || !hotels.Any())
            {
                hotels = GetMockHotels(city);
            }

            // Remove duplicates by Name
            hotels = hotels
                .GroupBy(h => h.Name)
                .Select(g => g.First())
                .Take(6)
                .ToList();

            return hotels;
        }

        // =========================================================
        // MOCK HOTELS DATA (With Beautiful Images)
        // =========================================================
        private List<RealHotel> GetMockHotels(string city)
        {
            var rng = new Random();
            var cityKey = city.ToLower();

            // Real Pakistan hotels with Unsplash professional images
            var mockDatabase = new Dictionary<string, List<RealHotel>>(StringComparer.OrdinalIgnoreCase)
    {
        { "hunza", new List<RealHotel> {
            new RealHotel { Id = "1", Name = "Hunza Serena Hotel", Address = "Karimabad, Hunza", Rating = 4.8m, PricePerNight = 25000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 456, Stars = "5" },
            new RealHotel { Id = "2", Name = "Eagle's Nest Hotel", Address = "Duikar, Hunza", Rating = 4.6m, PricePerNight = 18000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 234, Stars = "4" },
            new RealHotel { Id = "3", Name = "Hunza Darbar Hotel", Address = "Ganish Village, Hunza", Rating = 4.4m, PricePerNight = 12000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 189, Stars = "4" },
            new RealHotel { Id = "4", Name = "Lakeside Inn Hunza", Address = "Attabad Lake, Hunza", Rating = 4.3m, PricePerNight = 15000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=500", ReviewCount = 156, Stars = "4" },
            new RealHotel { Id = "5", Name = "Hilltop Hotel Hunza", Address = "Karimabad, Hunza", Rating = 4.1m, PricePerNight = 10000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1445019980597-93fa8acb246c?w=500", ReviewCount = 98, Stars = "3" },
        }},

        { "murree", new List<RealHotel> {
            new RealHotel { Id = "1", Name = "Pearl Continental Murree", Address = "Mall Road, Murree", Rating = 4.7m, PricePerNight = 22000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 567, Stars = "5" },
            new RealHotel { Id = "2", Name = "Shelton Hotel Murree", Address = "GPO Chowk, Murree", Rating = 4.2m, PricePerNight = 12000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 345, Stars = "4" },
            new RealHotel { Id = "3", Name = "Bhurban Pearl Continental", Address = "Bhurban, Murree", Rating = 4.9m, PricePerNight = 35000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 789, Stars = "5" },
        }},

        { "skardu", new List<RealHotel> {
            new RealHotel { Id = "1", Name = "Skardu Serena Hotel", Address = "Skardu City", Rating = 4.9m, PricePerNight = 35000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 678, Stars = "5" },
            new RealHotel { Id = "2", Name = "Shangrila Resort", Address = "Skardu", Rating = 4.7m, PricePerNight = 28000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 456, Stars = "5" },
            new RealHotel { Id = "3", Name = "PTDC Motel Skardu", Address = "Skardu", Rating = 4.3m, PricePerNight = 15000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 234, Stars = "4" },
        }},

        { "naran", new List<RealHotel> {
            new RealHotel { Id = "1", Name = "Naran Continental Hotel", Address = "Naran Bazaar", Rating = 4.4m, PricePerNight = 15000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 345, Stars = "4" },
            new RealHotel { Id = "2", Name = "River Garden Resort", Address = "River Side, Naran", Rating = 4.6m, PricePerNight = 18000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 278, Stars = "4" },
            new RealHotel { Id = "3", Name = "PTDC Motel Naran", Address = "Naran", Rating = 4.1m, PricePerNight = 12000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 189, Stars = "3" },
        }},

        { "swat", new List<RealHotel> {
            new RealHotel { Id = "1", Name = "Swat Serena Hotel", Address = "Saidu Sharif, Swat", Rating = 4.7m, PricePerNight = 22000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 456, Stars = "5" },
            new RealHotel { Id = "2", Name = "Hindukush Heights", Address = "Malam Jabba Road", Rating = 4.5m, PricePerNight = 16000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 234, Stars = "4" },
            new RealHotel { Id = "3", Name = "Shelton Hotel Swat", Address = "Mingora", Rating = 4.2m, PricePerNight = 10000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 178, Stars = "3" },
        }},

        { "lahore", new List<RealHotel> {
            new RealHotel { Id = "1", Name = "Pearl Continental Lahore", Address = "Mall Road, Lahore", Rating = 4.8m, PricePerNight = 28000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 890, Stars = "5" },
            new RealHotel { Id = "2", Name = "Avari Hotel Lahore", Address = "Mall Road, Lahore", Rating = 4.6m, PricePerNight = 22000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 678, Stars = "5" },
            new RealHotel { Id = "3", Name = "Nishat Hotel", Address = "Gulberg, Lahore", Rating = 4.5m, PricePerNight = 20000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 456, Stars = "4" },
        }},

        { "islamabad", new List<RealHotel> {
            new RealHotel { Id = "1", Name = "Serena Hotel Islamabad", Address = "Khayaban-e-Suhrwardy", Rating = 4.9m, PricePerNight = 35000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 1023, Stars = "5" },
            new RealHotel { Id = "2", Name = "Marriott Hotel Islamabad", Address = "Constitution Avenue", Rating = 4.8m, PricePerNight = 32000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 890, Stars = "5" },
            new RealHotel { Id = "3", Name = "Avari Hotel Islamabad", Address = "Main F-7", Rating = 4.6m, PricePerNight = 25000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 567, Stars = "4" },
        }},

        { "karachi", new List<RealHotel> {
            new RealHotel { Id = "1", Name = "Pearl Continental Karachi", Address = "Club Road", Rating = 4.7m, PricePerNight = 26000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 756, Stars = "5" },
            new RealHotel { Id = "2", Name = "Movenpick Hotel Karachi", Address = "Club Road", Rating = 4.6m, PricePerNight = 24000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 634, Stars = "5" },
            new RealHotel { Id = "3", Name = "Avari Tower Karachi", Address = "Fatima Jinnah Road", Rating = 4.4m, PricePerNight = 18000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 432, Stars = "4" },
        }},
    };

            // Return mock data for the city, or default fallback
            if (mockDatabase.ContainsKey(cityKey))
            {
                return mockDatabase[cityKey];
            }

            // Default fallback for any city
            return new List<RealHotel>
    {
        new RealHotel { Id = "1", Name = $"Pearl Continental {city}", Address = $"Main {city}", Rating = 4.5m, PricePerNight = 20000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1566073771259-6a8506099945?w=500", ReviewCount = 300, Stars = "5" },
        new RealHotel { Id = "2", Name = $"Serena Hotel {city}", Address = $"{city} Center", Rating = 4.3m, PricePerNight = 16000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=500", ReviewCount = 250, Stars = "4" },
        new RealHotel { Id = "3", Name = $"Avari Hotel {city}", Address = $"Main {city}", Rating = 4.1m, PricePerNight = 14000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?w=500", ReviewCount = 200, Stars = "4" },
        new RealHotel { Id = "4", Name = $"Holiday Inn {city}", Address = $"Civic Center {city}", Rating = 3.9m, PricePerNight = 10000, Currency = "PKR", ImageUrl = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?w=500", ReviewCount = 150, Stars = "3" },
    };
        }
    }
}