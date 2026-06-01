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
        private async Task<List<RealHotel>> GetHotelsDirectAsync(
            string city, string checkIn, string checkOut)
        {
            var hotels = new List<RealHotel>();
            var apiKey = "35f85c261fmsh0b3fdef51d3d998p1de5fajsn410ef6c10768";
            var host = "booking-com15.p.rapidapi.com";

            try
            {
                var client = new HttpClient();

                var destRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(
                        $"https://booking-com15.p.rapidapi.com/api/v1/hotels/searchDestination" +
                        $"?query={city}"),
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

                if (destId == null) return hotels;

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
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hotels Error: {ex.Message}");
            }

            return hotels;
        }
    }
}