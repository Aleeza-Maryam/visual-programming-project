using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;

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

            var bookings = await _context.bookings
                .Include(b => b.Transport)
                .Where(b => b.user_id == userId)
                .OrderByDescending(b => b.created_at)
                .ToListAsync();

            return View(bookings);
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