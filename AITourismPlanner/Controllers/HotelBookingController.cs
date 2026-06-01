using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;

namespace AITourismPlanner.Controllers
{
    public class HotelBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HotelBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // SEARCH HOTELS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Search(string city, DateTime? checkIn, DateTime? checkOut, int guests = 1)
        {
            ViewBag.City = city;
            ViewBag.CheckIn = checkIn ?? DateTime.Now.AddDays(1);
            ViewBag.CheckOut = checkOut ?? DateTime.Now.AddDays(3);
            ViewBag.Guests = guests;

            var query = _context.hotels
                .Include(h => h.Destination)
                .Include(h => h.HotelRooms)
                .AsQueryable();

            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(h => h.Destination != null &&
                    (h.Destination.city.Contains(city) || h.Destination.name.Contains(city)));
            }

            var hotels = await query.ToListAsync();
            return View(hotels);
        }

        // =========================================================
        // HOTEL DETAILS WITH ROOMS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(int id, DateTime? checkIn, DateTime? checkOut, int guests = 1)
        {
            var hotel = await _context.hotels
                .Include(h => h.Destination)
                .Include(h => h.HotelRooms)
                .Include(h => h.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(h => h.hotel_id == id);

            if (hotel == null)
                return NotFound();

            var checkInDate = checkIn ?? DateTime.Now.AddDays(1);
            var checkOutDate = checkOut ?? DateTime.Now.AddDays(3);
            var nights = (checkOutDate - checkInDate).Days;

            ViewBag.CheckIn = checkInDate;
            ViewBag.CheckOut = checkOutDate;
            ViewBag.Guests = guests;
            ViewBag.Nights = nights;

            // Calculate room availability
            foreach (var room in hotel.HotelRooms)
            {
                var bookedCount = await _context.hotel_bookings
                    .Where(b => b.room_id == room.room_id &&
                           b.booking_status == "Confirmed" &&
                           b.check_in < checkOutDate &&
                           b.check_out > checkInDate)
                    .CountAsync();

                room.available_rooms = (room.total_rooms ?? 0) - bookedCount;
            }

            return View(hotel);
        }

        // =========================================================
        // BOOK HOTEL - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Book(int roomId, DateTime checkIn, DateTime checkOut, int guests)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var room = await _context.hotel_rooms
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.room_id == roomId);

            if (room == null)
                return NotFound();

            var nights = (checkOut - checkIn).Days;
            var totalAmount = (room.room_price ?? 0) * nights;

            var model = new HotelBookingViewModel
            {
                RoomId = roomId,
                Room = room,
                Hotel = room.Hotel,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Nights = nights,
                Guests = guests,
                TotalAmount = totalAmount
            };

            return View(model);
        }

        // =========================================================
        // BOOK HOTEL - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(HotelBookingViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var room = await _context.hotel_rooms
                    .Include(r => r.Hotel)
                    .FirstOrDefaultAsync(r => r.room_id == model.RoomId);

                if (room == null)
                    return NotFound();

                var booking = new HotelBooking
                {
                    user_id = userId.Value,
                    hotel_id = room.hotel_id,
                    room_id = model.RoomId,
                    check_in = model.CheckIn,
                    check_out = model.CheckOut,
                    total_amount = model.TotalAmount,
                    booking_status = "Pending",
                    booking_date = DateTime.Now
                };

                _context.hotel_bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Create notification
                var notification = new Notification
                {
                    user_id = userId.Value,
                    title = "Hotel Booking Created",
                    message = $"Your booking at {room.Hotel?.hotel_name ?? "Hotel"} is pending confirmation.",
                    created_at = DateTime.Now
                };
                _context.notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Booking created successfully! Please complete payment to confirm.";
                return RedirectToAction("MyBookings");
            }

            return View(model);
        }

        // =========================================================
        // MY BOOKINGS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var bookings = await _context.hotel_bookings
                .Include(b => b.Hotel)
                .Include(b => b.HotelRoom)
                .Where(b => b.user_id == userId)
                .OrderByDescending(b => b.booking_date)
                .ToListAsync();

            return View(bookings);
        }

        // =========================================================
        // CANCEL BOOKING
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Please login" });

            var booking = await _context.hotel_bookings.FindAsync(id);

            if (booking == null)
                return Json(new { success = false, message = "Booking not found" });

            if (booking.user_id != userId)
                return Json(new { success = false, message = "Unauthorized" });

            booking.booking_status = "Cancelled";
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Booking cancelled successfully" });
        }

        // =========================================================
        // BOOKING DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> BookingDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var booking = await _context.hotel_bookings
                .Include(b => b.Hotel)
                .Include(b => b.HotelRoom)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.booking_id == id);

            if (booking == null)
                return NotFound();

            if (booking.user_id != userId)
                return Forbid();

            return View(booking);
        }
    }

    // =========================================================

    // VIEW MODEL
    // =========================================================
    public class HotelBookingViewModel
    {
        public int RoomId { get; set; }
        public HotelRoom Room { get; set; }
        public Hotel Hotel { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Nights { get; set; }
        public int Guests { get; set; }
        public decimal TotalAmount { get; set; }

    }
}
