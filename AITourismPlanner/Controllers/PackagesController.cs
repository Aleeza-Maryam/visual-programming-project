using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using AITourismPlanner.Services;   // ✅ Add this

namespace AITourismPlanner.Controllers
{
    public class PackagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPexelsImageService _pexelsService;   // ✅ Add this

        // ✅ Updated constructor
        public PackagesController(ApplicationDbContext context, IPexelsImageService pexelsService)
        {
            _context = context;
            _pexelsService = pexelsService;
        }

        // =========================================================
        // PACKAGES LISTING
        // =========================================================
        public async Task<IActionResult> Index(string type = "all", string sort = "price_asc")
        {
            var query = _context.packages.Where(p => p.is_active).AsQueryable();

            if (type != "all")
            {
                query = query.Where(p => p.package_type != null && p.package_type == type);
            }

            query = sort switch
            {
                "price_desc" => query.OrderByDescending(p => p.price_per_person),
                "duration_asc" => query.OrderBy(p => p.duration_nights),
                "rating_desc" => query.OrderByDescending(p => p.PackageReviews != null ? p.PackageReviews.Average(r => r.rating ?? 0) : 0),
                _ => query.OrderBy(p => p.price_per_person)
            };

            var packages = await query.ToListAsync();

            ViewBag.DestinationsList = await _context.packages
                .Where(p => p.is_active)
                .Select(p => p.destination_name)
                .Distinct()
                .ToListAsync();

            ViewBag.SelectedType = type;
            ViewBag.SelectedSort = sort;
            ViewBag.PackageTypes = new[] { "Budget", "Standard", "Premium", "Honeymoon", "Family" };

            return View(packages);
        }

        // =========================================================
        // PACKAGE DETAILS
        // =========================================================
        public async Task<IActionResult> Details(int id)
        {
            var package = await _context.packages
                .Include(p => p.PackageReviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.package_id == id);

            if (package == null)
                return NotFound();

            double avgRating = 0;
            if (package.PackageReviews != null && package.PackageReviews.Any())
            {
                avgRating = (double)(package.PackageReviews.Average(r => r.rating ?? 0));
            }

            ViewBag.AverageRating = avgRating;
            ViewBag.TotalReviews = package.PackageReviews?.Count ?? 0;

            return View(package);
        }

        // =========================================================
        // BOOK PACKAGE - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Book(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var package = await _context.packages.FindAsync(id);
            if (package == null)
                return NotFound();

            var viewModel = new PackageBookingViewModel
            {
                Package = package,
                TravelDate = DateTime.Now.AddDays(7),
                NumberOfAdults = 1,
                NumberOfChildren = 0,
                PricePerPerson = package.price_per_person,
                GroupDiscountPercent = package.group_discount_percent
            };

            return View(viewModel);
        }

        // =========================================================
        // BOOK PACKAGE - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(PackageBookingViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var package = await _context.packages.FindAsync(model.PackageId);
                if (package == null)
                    return NotFound();

                int totalPersons = model.NumberOfAdults + model.NumberOfChildren;
                decimal baseTotal = package.price_per_person * totalPersons;

                decimal discount = 0;
                if (totalPersons >= 4 && package.group_discount_percent > 0)
                {
                    discount = baseTotal * (package.group_discount_percent / 100);
                }

                decimal finalTotal = baseTotal - discount;

                var reference = "PKG" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999);

                var booking = new PackageBooking
                {
                    user_id = userId.Value,
                    package_id = model.PackageId,
                    booking_reference = reference,
                    travel_date = model.TravelDate,
                    number_of_adults = model.NumberOfAdults,
                    number_of_children = model.NumberOfChildren,
                    special_requests = model.SpecialRequests,
                    dietary_needs = model.DietaryNeeds,
                    room_preferences = model.RoomPreferences,
                    total_price = baseTotal,
                    discount_applied = discount,
                    final_price = finalTotal,
                    booking_status = "Confirmed",
                    payment_status = "Pending",
                    booking_date = DateTime.Now
                };

                _context.package_bookings.Add(booking);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Package booked! Reference: {reference}";
                return RedirectToAction("MyPackageBookings");
            }

            model.Package = await _context.packages.FindAsync(model.PackageId);
            return View(model);
        }

        // =========================================================
        // MY PACKAGE BOOKINGS
        // =========================================================
        public async Task<IActionResult> MyPackageBookings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            try
            {
                var bookings = await _context.package_bookings
                    .Include(b => b.Package)
                    .Where(b => b.user_id == userId)
                    .OrderByDescending(b => b.booking_date)
                    .ToListAsync();

                return View(bookings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                TempData["Error"] = "Bookings feature is being set up. Please try again later.";
                return View(new List<PackageBooking>());
            }
        }

        // =========================================================
        // ADD PACKAGE REVIEW
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> AddReview(int packageId, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Please login first" });

            var review = new PackageReview
            {
                user_id = userId.Value,
                package_id = packageId,
                rating = rating,
                comment = comment,
                created_at = DateTime.Now
            };

            _context.package_reviews.Add(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Review added!" });
        }

        // =========================================================
        // CREATE PACKAGE BOOKING - API
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CreatePackageBooking(
            int packageId, DateTime travelDate, int numberOfAdults, int numberOfChildren,
            string specialRequests, string dietaryNeeds, string roomPreferences,
            string guestName, string guestPhone, string guestEmail, decimal totalPrice)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Please login first" });

            var package = await _context.packages.FindAsync(packageId);
            if (package == null)
                return Json(new { success = false, message = "Package not found" });

            var reference = "PKG" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999);

            var booking = new PackageBooking
            {
                user_id = userId.Value,
                package_id = packageId,
                booking_reference = reference,
                travel_date = travelDate,
                number_of_adults = numberOfAdults,
                number_of_children = numberOfChildren,
                special_requests = specialRequests,
                dietary_needs = dietaryNeeds,
                room_preferences = roomPreferences,
                total_price = totalPrice,
                discount_applied = 0,
                final_price = totalPrice,
                booking_status = "Confirmed",
                payment_status = "Pending",
                booking_date = DateTime.Now
            };

            _context.package_bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Json(new { success = true, reference = reference });
        }

        // =========================================================
        // DOWNLOAD VOUCHER
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> DownloadVoucher(string reference)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var booking = await _context.package_bookings
                .Include(b => b.Package)
                .FirstOrDefaultAsync(b => b.booking_reference == reference && b.user_id == userId);

            if (booking == null)
                return NotFound();

            var html = $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8'>
        <title>Tour Package Voucher</title>
        <style>
            body {{ font-family: Arial, sans-serif; padding: 40px; }}
            .voucher {{ border: 2px solid #4CAF50; border-radius: 10px; padding: 20px; max-width: 800px; margin: 0 auto; }}
            .header {{ text-align: center; border-bottom: 1px solid #ddd; padding-bottom: 15px; }}
            .company-name {{ color: #4CAF50; font-size: 28px; font-weight: bold; }}
            .title {{ font-size: 24px; font-weight: bold; text-align: center; margin: 20px 0; }}
            .details {{ margin: 20px 0; }}
            .row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #eee; }}
            .label {{ font-weight: bold; width: 40%; }}
            .value {{ width: 60%; }}
            .footer {{ text-align: center; margin-top: 30px; font-size: 12px; color: #666; }}
            .status-confirmed {{ color: green; font-weight: bold; }}
            button {{ background: #4CAF50; color: white; padding: 10px 20px; border: none; border-radius: 5px; cursor: pointer; font-size: 16px; margin-top: 20px; }}
            button:hover {{ background: #45a049; }}
        </style>
    </head>
    <body>
        <div class='voucher' id='voucher'>
            <div class='header'>
                <div class='company-name'>AI Tourism Planner</div>
                <p>Tour Confirmation Voucher</p>
            </div>
            <div class='title'>✨ Booking Confirmed ✨</div>
            <div class='details'>
                <div class='row'><div class='label'>Booking Reference:</div><div class='value'><strong>{booking.booking_reference}</strong></div></div>
                <div class='row'><div class='label'>Status:</div><div class='value'><span class='status-confirmed'>Confirmed</span></div></div>
                <div class='row'><div class='label'>Package Name:</div><div class='value'>{booking.Package?.package_name}</div></div>
                <div class='row'><div class='label'>Destination:</div><div class='value'>{booking.Package?.destination_name}</div></div>
                <div class='row'><div class='label'>Travel Date:</div><div class='value'>{booking.travel_date.ToString("dd MMM yyyy")}</div></div>
                <div class='row'><div class='label'>Duration:</div><div class='value'>{booking.Package?.duration_days} Days / {booking.Package?.duration_nights} Nights</div></div>
                <div class='row'><div class='label'>Number of Adults:</div><div class='value'>{booking.number_of_adults}</div></div>
                <div class='row'><div class='label'>Number of Children:</div><div class='value'>{booking.number_of_children}</div></div>
                <div class='row'><div class='label'>Hotel:</div><div class='value'>{booking.Package?.hotel_name} ({booking.Package?.hotel_stars}⭐)</div></div>
                <div class='row'><div class='label'>Transport:</div><div class='value'>{booking.Package?.transport_type}</div></div>
                <div class='row'><div class='label'>Meals Included:</div><div class='value'>{booking.Package?.meals_included}</div></div>
                <div class='row'><div class='label'>Total Price:</div><div class='value'><strong style='color:#4CAF50; font-size:18px;'>PKR {booking.final_price:N0}</strong></div></div>
            </div>
            <div class='footer'>
                <p>Thank you for booking with AI Tourism Planner!</p>
                <p>For any queries, contact us at support@aitourism.com | +92 300 1234567</p>
            </div>
        </div>
        <div style='text-align:center;'>
            <button onclick='window.print();'>🖨️ Print / Save as PDF</button>
        </div>
        <script>
            setTimeout(function() {{ window.print(); }}, 500);
        </script>
    </body>
    </html>";

            return Content(html, "text/html");
        }

        // =========================================================
        // CANCEL PACKAGE BOOKING
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CancelPackageBooking(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var booking = await _context.package_bookings.FindAsync(id);

            if (booking == null || booking.user_id != userId)
                return Json(new { success = false, message = "Booking not found" });

            if (booking.travel_date <= DateTime.Now.AddDays(1))
                return Json(new { success = false, message = "Cannot cancel booking within 24 hours of travel date" });

            booking.booking_status = "Cancelled";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Booking cancelled successfully!" });
        }

        // =========================================================
        // CANCEL BOOKING (Alias)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var booking = await _context.package_bookings.FindAsync(bookingId);

            if (booking == null || booking.user_id != userId)
                return Json(new { success = false, message = "Booking not found" });

            if (booking.travel_date <= DateTime.Now.AddDays(1))
                return Json(new { success = false, message = "Cannot cancel booking within 24 hours of travel date" });

            booking.booking_status = "Cancelled";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Booking cancelled successfully!" });
        }

        // =========================================================
        // ✅ NEW ACTION: Refresh all package images from Pexels
        // Run this once: https://localhost:7178/Packages/RefreshPackageImages
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> RefreshPackageImages()
        {
            var packages = await _context.packages.Where(p => p.is_active).ToListAsync();
            int updated = 0;

            foreach (var pkg in packages)
            {
                try
                {
                    string imageUrl = await _pexelsService.GetImageForDestinationAsync(pkg.destination_name);

                    if (!string.IsNullOrEmpty(imageUrl) && pkg.cover_image != imageUrl)
                    {
                        pkg.cover_image = imageUrl;
                        updated++;
                    }

                    await Task.Delay(200); // Respect rate limit
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating {pkg.package_name}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            return Content($"✅ Updated {updated} out of {packages.Count} packages with Pexels images.");
        }
    }

    public class PackageBookingViewModel
    {
        public int PackageId { get; set; }
        public Package Package { get; set; }
        public DateTime TravelDate { get; set; }
        public int NumberOfAdults { get; set; } = 1;
        public int NumberOfChildren { get; set; } = 0;
        public decimal PricePerPerson { get; set; }
        public decimal GroupDiscountPercent { get; set; }
        public string SpecialRequests { get; set; }
        public string DietaryNeeds { get; set; }
        public string RoomPreferences { get; set; }

        public decimal TotalPrice => PricePerPerson * (NumberOfAdults + NumberOfChildren);
        public decimal DiscountAmount => TotalPrice * (GroupDiscountPercent / 100);
        public decimal FinalPrice => TotalPrice - DiscountAmount;
    }
}