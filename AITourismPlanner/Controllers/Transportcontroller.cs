using AITourismPlanner.Models;
using Microsoft.AspNetCore.Http; // Added for Session extensions
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Added for ToListAsync() and Entity Framework operations
using AITourismPlanner.Data;
using AITourismPlanner.Data;
namespace AITourismPlanner.Controllers
{
    // Inherited from Controller to enable View(), HttpContext, and Json()
    public class TransportController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor to inject Database Context
        public TransportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // INDEX - All Transport Options
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var transports = await _context.transports
      .OrderBy(t => t.departure_city)
      .ToListAsync();
            return View(transports);
        }

        // =========================================================
        // CREATE BOOKING - Handle Post Request
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CreateTransportBooking(int transportId, int seats, DateTime travelDate, decimal totalPrice)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Please login first" });

            var transport = await _context.transports.FindAsync(transportId);
            if (transport == null)
                return Json(new { success = false, message = "Transport not found" });

            if (transport.available_seats < seats)
                return Json(new { success = false, message = "Not enough seats available" });

            var reference = "TRP" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999);

            var booking = new Booking
            {
                user_id = userId.Value,
                destination_name = $"{transport.departure_city} to {transport.arrival_city}",
                transport_id = transportId,
                check_in_date = travelDate,
                check_out_date = travelDate,
                number_of_guests = seats,
                total_price = totalPrice,
                booking_status = "Confirmed",
                payment_status = "Pending",
                booking_reference = reference,
                created_at = DateTime.Now
            };

            _context.bookings.Add(booking);

            // Update available seats
            transport.available_seats -= seats;

            await _context.SaveChangesAsync();

            return Json(new { success = true, reference = reference });
        }
    }
}