using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.ViewModels;
namespace AITourismPlanner.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            // Get hotel/transport bookings
            var hotelBookings = await _context.bookings
                .Include(b => b.Transport)
                .Where(b => b.user_id == userId)
                .Select(b => new CombinedBooking
                {
                    BookingId = b.booking_id,
                    BookingType = "Hotel & Transport",
                    DestinationName = b.destination_name,
                    HotelName = b.hotel_name,
                    TransportName = b.Transport != null ? b.Transport.company_name : null,
                    CheckInDate = b.check_in_date,
                    CheckOutDate = b.check_out_date,
                    TravelDate = b.check_in_date,
                    NumberOfGuests = b.number_of_guests,
                    TotalPrice = b.total_price ?? 0,
                    BookingStatus = b.booking_status,
                    PaymentStatus = b.payment_status,
                    BookingReference = b.booking_reference,
                    CreatedAt = b.created_at
                })
                .ToListAsync();

            // Get package bookings
            var packageBookings = await _context.package_bookings
                .Include(b => b.Package)
                .Where(b => b.user_id == userId)
                .Select(b => new CombinedBooking
                {
                    BookingId = b.booking_id,
                    BookingType = "Package",
                    PackageName = b.Package != null ? b.Package.package_name : null,
                    DestinationName = b.Package != null ? b.Package.destination_name : null,
                    CheckInDate = b.travel_date,
                    CheckOutDate = b.travel_date.AddDays(b.Package != null ? b.Package.duration_days : 1),
                    TravelDate = b.travel_date,
                    NumberOfGuests = b.number_of_adults + b.number_of_children,
                    TotalPrice = b.final_price,
                    BookingStatus = b.booking_status,
                    PaymentStatus = b.payment_status,
                    BookingReference = b.booking_reference,
                    CreatedAt = b.booking_date
                })
                .ToListAsync();

            // Combine both lists
            var allBookings = hotelBookings.Concat(packageBookings)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            return View(allBookings);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var booking = await _context.bookings.FindAsync(id);

            if (booking == null || booking.user_id != userId)
                return Json(new { success = false });

            // Cancellation policy — 24 ghante pehle cancel kar sakte hain
            if (booking.check_in_date <= DateTime.Now.AddDays(1))
                return Json(new
                {
                    success = false,
                    message = "Cannot cancel within 24 hours of check-in"
                });

            booking.booking_status = "Cancelled";
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}