using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using System.Security.Cryptography;
using System.Text;

namespace AITourismPlanner.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =========================================================
        // CHECK IF USER IS ADMIN
        // =========================================================
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        // =========================================================
        // DASHBOARD
        // =========================================================
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Get monthly revenue data
            var monthlyRevenue = new Dictionary<string, decimal>();
            var months = Enumerable.Range(1, 12).Select(m => new DateTime(DateTime.Now.Year, m, 1));

            foreach (var month in months)
            {
                var revenue = await _context.bookings
                    .Where(b => b.created_at.Year == DateTime.Now.Year && b.created_at.Month == month.Month)
                    .SumAsync(b => b.total_price ?? 0);

                monthlyRevenue[month.ToString("MMM")] = revenue;
            }

            // Get last 6 months trend data
            var last6Months = new List<DateTime>();
            for (int i = 5; i >= 0; i--)
            {
                last6Months.Add(DateTime.Now.AddMonths(-i));
            }

            var trendData = new Dictionary<string, decimal>();
            foreach (var month in last6Months)
            {
                var revenue = await _context.bookings
                    .Where(b => b.created_at.Year == month.Year && b.created_at.Month == month.Month)
                    .SumAsync(b => b.total_price ?? 0);
                trendData[month.ToString("MMM yyyy")] = revenue;
            }

            // Get package bookings revenue as well
            var packageRevenue = await _context.package_bookings
                .Where(b => b.booking_status == "Confirmed")
                .SumAsync(b => b.final_price);

            var totalRevenue = (await _context.bookings.SumAsync(b => b.total_price ?? 0)) + packageRevenue;

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.users.CountAsync(),
                TotalCustomers = await _context.users.Where(u => u.role_id == 2).CountAsync(),
                TotalBookings = await _context.bookings.CountAsync() + await _context.package_bookings.CountAsync(),
                PendingBookings = await _context.bookings.Where(b => b.booking_status == "Pending").CountAsync(),
                TotalRevenue = totalRevenue,
                TotalDestinations = await _context.destinations.CountAsync(),
                TotalHotels = await _context.hotels.CountAsync(),
                RecentBookings = await _context.bookings
                    .Include(b => b.User)
                    .OrderByDescending(b => b.created_at)
                    .Take(5)
                    .ToListAsync(),
                MonthlyRevenue = monthlyRevenue,
                RevenueTrend = trendData
            };

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            return View(viewModel);
        }
        // =========================================================
        // MANAGE TRANSPORT
        // =========================================================
        public async Task<IActionResult> Transport()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var transports = await _context.transports
                .OrderByDescending(t => t.transport_id)
                .ToListAsync();

            return View(transports);
        }

        [HttpGet]
        public IActionResult AddTransport()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTransport(Transport model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                model.created_at = DateTime.Now;
                _context.transports.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Transport added successfully!";
                return RedirectToAction("Transport");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditTransport(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var transport = await _context.transports.FindAsync(id);
            if (transport == null) return NotFound();
            return View(transport);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTransport(int id, Transport model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (id != model.transport_id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Transport updated successfully!";
                return RedirectToAction("Transport");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTransport(int id)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var transport = await _context.transports.FindAsync(id);
            if (transport != null)
            {
                _context.transports.Remove(transport);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        // =========================================================
        // MANAGE USERS
        // =========================================================
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var users = await _context.users
                .Include(u => u.Role)
                .OrderByDescending(u => u.user_id)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var user = await _context.users.FindAsync(id);
            if (user != null && user.email != "admin@aitourism.com")
            {
                _context.users.Remove(user);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Cannot delete admin user" });
        }

        // =========================================================
        // MANAGE BOOKINGS
        // =========================================================
        public async Task<IActionResult> Bookings()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var bookings = await _context.bookings
                .Include(b => b.User)
                .Include(b => b.Transport)
               .OrderByDescending(b => b.created_at)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(int id, string status)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var booking = await _context.bookings.FindAsync(id);
            if (booking != null)
            {
                booking.booking_status = status;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // =========================================================
        // MANAGE PACKAGES
        // =========================================================
        public async Task<IActionResult> Packages()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var packages = await _context.packages
                .OrderByDescending(p => p.package_id)
                .ToListAsync();

            return View(packages);
        }

        [HttpGet]
        public IActionResult AddPackage()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPackage(Package model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                model.created_at = DateTime.Now;
                _context.packages.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Package added successfully!";
                return RedirectToAction("Packages");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditPackage(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var package = await _context.packages.FindAsync(id);
            if (package == null) return NotFound();
            return View(package);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPackage(int id, Package model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (id != model.package_id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Package updated successfully!";
                return RedirectToAction("Packages");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeletePackage(int id)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var package = await _context.packages.FindAsync(id);
            if (package != null)
            {
                _context.packages.Remove(package);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // =========================================================
        // MANAGE DESTINATIONS
        // =========================================================
        public async Task<IActionResult> Destinations()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var destinations = await _context.destinations
                .Include(d => d.Category)
                .OrderByDescending(d => d.destination_id)
                .ToListAsync();

            return View(destinations);
        }

        [HttpGet]
        public async Task<IActionResult> AddDestination()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            ViewBag.Categories = await _context.categories.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDestination(Destination model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.destinations.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Destination added successfully!";
                return RedirectToAction("Destinations");
            }
            ViewBag.Categories = await _context.categories.ToListAsync();
            return View(model);
        }

        // =========================================================
        // MANAGE HOTELS
        // =========================================================
        public async Task<IActionResult> Hotels()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var hotels = await _context.hotels
                .Include(h => h.Destination)
                .OrderByDescending(h => h.hotel_id)
                .ToListAsync();

            return View(hotels);
        }

        // =========================================================
        // MANAGE REVIEWS
        // =========================================================
        public async Task<IActionResult> Reviews()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var reviews = await _context.reviews
                .Include(r => r.User)
                .Include(r => r.Destination)
                .OrderByDescending(r => r.review_date)
                .ToListAsync();

            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var review = await _context.reviews.FindAsync(id);
            if (review != null)
            {
                _context.reviews.Remove(review);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // =========================================================
        // PACKAGE BOOKINGS (for packages)
        // =========================================================
        public async Task<IActionResult> PackageBookings()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var bookings = await _context.package_bookings
                .Include(b => b.User)
                .Include(b => b.Package)
                .OrderByDescending(b => b.booking_date)
                .ToListAsync();

            return View(bookings);
        }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalDestinations { get; set; }
        public int TotalHotels { get; set; }
        public List<Booking> RecentBookings { get; set; } = new();
        public Dictionary<string, decimal> MonthlyRevenue { get; set; } = new();  // ✅ Add this
        public Dictionary<string, decimal> RevenueTrend { get; set; } = new();     // ✅ Add this
    }
}