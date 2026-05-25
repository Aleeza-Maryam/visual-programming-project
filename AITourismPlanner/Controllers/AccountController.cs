using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using AITourismPlanner.ViewModels;

namespace AITourismPlanner.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // =========================================================
        // LOGIN - GET
        // =========================================================
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // =========================================================
        // LOGIN - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var hashedPassword = HashPassword(model.Password);

                var user = await _context.users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.email == model.Email && u.password_hash == hashedPassword && u.status == "Active");

                if (user != null)
                {
                    // Set session variables
                    HttpContext.Session.SetInt32("UserId", user.user_id);
                    HttpContext.Session.SetString("UserName", user.full_name);
                    HttpContext.Session.SetString("UserEmail", user.email);
                    HttpContext.Session.SetString("UserRole", user.Role?.role_name ?? "Customer");

                    // Set cookie for remember me
                    if (model.RememberMe)
                    {
                        CookieOptions options = new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(30),
                            HttpOnly = true,
                            IsEssential = true
                        };
                        Response.Cookies.Append("UserEmail", user.email, options);
                    }

                    // Log activity
                    await LogUserActivity(user.user_id, "Login", "User logged in successfully");

                    TempData["Success"] = $"Welcome back, {user.full_name}!";

                    // Redirect based on role
                    if (user.Role?.role_name == "Admin")
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid email or password OR account is blocked.");
                }
            }
            return View(model);
        }

        // =========================================================
        // REGISTER - GET
        // =========================================================
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // =========================================================
        // REGISTER - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _context.users.FirstOrDefaultAsync(u => u.email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email already registered!");
                    return View(model);
                }

                // Check if passwords match
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Passwords do not match!");
                    return View(model);
                }

                // Create new user
                var user = new User
                {
                    full_name = model.FullName,
                    email = model.Email,
                    password_hash = HashPassword(model.Password),
                    phone = model.Phone,
                    gender = model.Gender,
                    date_of_birth = model.DateOfBirth,
                    role_id = 2, // Customer role (assuming 1=Admin, 2=Customer, 3=Agent)
                    status = "Active",
                    created_at = DateTime.Now
                };

                _context.users.Add(user);
                await _context.SaveChangesAsync();

                // Create default preferences
                var preferences = new UserPreference
                {
                    user_id = user.user_id,
                    preferred_budget = 50000,
                    favorite_category = "Adventure",
                    preferred_transport = "Any",
                    preferred_season = "Summer"
                };
                _context.user_preferences.Add(preferences);
                await _context.SaveChangesAsync();

                // Log activity
                await LogUserActivity(user.user_id, "Register", "New user registered");

                TempData["Success"] = "Registration successful! Please login.";

                // Auto login after registration
                HttpContext.Session.SetInt32("UserId", user.user_id);
                HttpContext.Session.SetString("UserName", user.full_name);
                HttpContext.Session.SetString("UserEmail", user.email);
                HttpContext.Session.SetString("UserRole", "Customer");

                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        // =========================================================
        // LOGOUT
        // =========================================================
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                await LogUserActivity(userId.Value, "Logout", "User logged out");
            }

            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserEmail");

            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // =========================================================
        // USER DASHBOARD
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.user_id == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var dashboardData = new DashboardViewModel
            {
                User = user,
                UpcomingTrips = await _context.trips
                    .Include(t => t.Destination)
                    .Where(t => t.user_id == userId && t.start_date >= DateTime.Now)
                    .OrderBy(t => t.start_date)
                    .Take(3)
                    .ToListAsync(),

                RecentBookings = await _context.hotel_bookings
                    .Include(hb => hb.Hotel)
                    .Where(hb => hb.user_id == userId)
                    .OrderByDescending(hb => hb.booking_date)
                    .Take(5)
                    .ToListAsync(),

                TotalTrips = await _context.trips.CountAsync(t => t.user_id == userId),
                TotalBookings = await _context.hotel_bookings.CountAsync(hb => hb.user_id == userId),
                TotalSpent = await _context.hotel_bookings
                    .Where(hb => hb.user_id == userId && hb.total_amount.HasValue)
                    .SumAsync(hb => hb.total_amount ?? 0),

                WishlistCount = await _context.favorites.CountAsync(f => f.user_id == userId),

                Recommendations = await _context.ai_recommendations
                    .Where(r => r.user_id == userId)
                    .OrderByDescending(r => r.generated_at)
                    .Take(3)
                    .ToListAsync(),

                Notifications = await _context.notifications
                    .Where(n => n.user_id == userId && !n.is_read)
                    .OrderByDescending(n => n.created_at)
                    .Take(5)
                    .ToListAsync()
            };

            return View(dashboardData);
        }

        // =========================================================
        // PROFILE
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.users
                .Include(u => u.UserPreference)
                .FirstOrDefaultAsync(u => u.user_id == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ProfileViewModel
            {
                UserId = user.user_id,
                FullName = user.full_name,
                Email = user.email,
                Phone = user.phone,
                Gender = user.gender,
                DateOfBirth = user.date_of_birth,
                ProfileImage = user.profile_image,
                PreferredBudget = user.UserPreference?.preferred_budget ?? 50000,
                FavoriteCategory = user.UserPreference?.favorite_category ?? "Adventure",
                PreferredTransport = user.UserPreference?.preferred_transport ?? "Any",
                PreferredSeason = user.UserPreference?.preferred_season ?? "Summer"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                var user = await _context.users.FindAsync(userId);
                if (user != null)
                {
                    user.full_name = model.FullName;
                    user.phone = model.Phone;
                    user.gender = model.Gender;
                    user.date_of_birth = model.DateOfBirth;

                    // Update preferences
                    var preferences = await _context.user_preferences
                        .FirstOrDefaultAsync(up => up.user_id == userId);

                    if (preferences != null)
                    {
                        preferences.preferred_budget = model.PreferredBudget;
                        preferences.favorite_category = model.FavoriteCategory;
                        preferences.preferred_transport = model.PreferredTransport;
                        preferences.preferred_season = model.PreferredSeason;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Profile updated successfully!";
                }
            }

            return RedirectToAction("Profile");
        }

        // =========================================================
        // CHANGE PASSWORD
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            var user = await _context.users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var currentHashed = HashPassword(model.CurrentPassword);
            if (user.password_hash != currentHashed)
            {
                return Json(new { success = false, message = "Current password is incorrect" });
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                return Json(new { success = false, message = "New passwords do not match" });
            }

            user.password_hash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully" });
        }

        // =========================================================
        // FORGOT PASSWORD
        // =========================================================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.users.FirstOrDefaultAsync(u => u.email == model.Email);
                if (user != null)
                {
                    // Generate reset token
                    var resetToken = GenerateResetToken();

                    // Store token in session or database
                    HttpContext.Session.SetString("ResetToken_" + user.email, resetToken);

                    // In production, send email here
                    TempData["Success"] = $"Password reset link sent to {model.Email}. Use token: {resetToken}";

                    return RedirectToAction("ResetPassword", new { email = model.Email });
                }
                else
                {
                    ModelState.AddModelError("", "Email not found");
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var storedToken = HttpContext.Session.GetString("ResetToken_" + model.Email);

                if (storedToken == model.Token)
                {
                    var user = await _context.users.FirstOrDefaultAsync(u => u.email == model.Email);
                    if (user != null)
                    {
                        user.password_hash = HashPassword(model.NewPassword);
                        await _context.SaveChangesAsync();

                        HttpContext.Session.Remove("ResetToken_" + model.Email);
                        TempData["Success"] = "Password reset successful! Please login.";

                        return RedirectToAction("Login");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid or expired token");
                }
            }
            return View(model);
        }

        // =========================================================
        // FAVORITES
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var favorites = await _context.favorites
                .Include(f => f.Destination)
                .Where(f => f.user_id == userId)
                .ToListAsync();

            return View(favorites);
        }

        [HttpPost]
        public async Task<IActionResult> AddToFavorites(int destinationId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var existing = await _context.favorites
                .FirstOrDefaultAsync(f => f.user_id == userId && f.destination_id == destinationId);

            if (existing == null)
            {
                var favorite = new Favorite
                {
                    user_id = userId,
                    destination_id = destinationId
                };
                _context.favorites.Add(favorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Added to favorites" });
            }

            return Json(new { success = false, message = "Already in favorites" });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int destinationId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false });
            }

            var favorite = await _context.favorites
                .FirstOrDefaultAsync(f => f.user_id == userId && f.destination_id == destinationId);

            if (favorite != null)
            {
                _context.favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        // =========================================================
        // HELPER METHODS
        // =========================================================
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private string GenerateResetToken()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task LogUserActivity(int userId, string activityType, string description)
        {
            var log = new AdminLog
            {
                admin_id = userId,
                action = $"{activityType}: {description}",
                created_at = DateTime.Now
            };
            _context.admin_logs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}