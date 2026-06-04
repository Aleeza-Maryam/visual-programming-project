using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AITourismPlanner.Data;
using AITourismPlanner.Models;
using AITourismPlanner.Services;
using AITourismPlanner.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using MailKit.Net.Smtp;
using MimeKit;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AITourismPlanner.Controllers
{
    public class TripsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIRecommendationService _aiService;
        private readonly IItineraryGenerator _itineraryGenerator;

        public TripsController(
            ApplicationDbContext context,
            IAIRecommendationService aiService,
            IItineraryGenerator itineraryGenerator)
        {
            _context = context;
            _aiService = aiService;
            _itineraryGenerator = itineraryGenerator;
        }

        // =========================================================
        // PLAN TRIP - AI Trip Planner
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> PlanTrip(int? destinationId = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var model = new TripPlannerViewModel
            {
                Destinations = await _context.destinations
                    .Select(d => new { d.destination_id, d.name })
                    .ToListAsync(),
                SelectedDestinationId = destinationId,
                StartDate = DateTime.Now.AddDays(7),
                EndDate = DateTime.Now.AddDays(10),
                Budget = 50000,
                Travelers = 2
            };

            // Get AI recommendations for logged-in users
            if (userId.HasValue)
            {
                model.Recommendations = await _aiService.GetRecommendationsAsync(userId.Value);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlanTrip(TripPlannerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get best destination using AI if not selected
                int destinationId = model.SelectedDestinationId ?? 0;
                string destinationName = "";

                if (destinationId == 0 && !string.IsNullOrEmpty(model.Interests))
                {
                    var bestMatch = await _aiService.GetBestMatchAsync(
                        model.Budget ?? 50000,
                        (model.EndDate - model.StartDate).Days + 1,
                        model.Interests
                    );

                    if (bestMatch != null)
                    {
                        destinationId = bestMatch.destination_id;
                        destinationName = bestMatch.name;
                    }
                }

                if (destinationId == 0)
                {
                    ModelState.AddModelError("", "Please select a destination or provide interests for AI recommendation");
                    model.Destinations = await _context.destinations
                        .Select(d => new { d.destination_id, d.name })
                        .ToListAsync();
                    return View(model);
                }

                // Generate trip plan
                var trip = await _itineraryGenerator.GenerateTripPlanAsync(
                    userId.Value,
                    destinationId,
                    model.StartDate,
                    model.EndDate,
                    model.Budget ?? 50000
                );

                // Save AI recommendation
                await _aiService.SaveRecommendationAsync(
                    userId.Value,
                    destinationName,
                    $"AI recommended based on budget of PKR {model.Budget:N0} and interests: {model.Interests}"
                );

                TempData["Success"] = "Your AI trip plan has been created!";
                return RedirectToAction("Details", new { id = trip.trip_id });
            }

            model.Destinations = await _context.destinations
                .Select(d => new { d.destination_id, d.name })
                .ToListAsync();
            return View(model);
        }

        // =========================================================
        // TRIP DETAILS
        // =========================================================
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var trip = await _context.trips
                .Include(t => t.Destination)
                .Include(t => t.Itineraries)
                .FirstOrDefaultAsync(t => t.trip_id == id);

            if (trip == null)
            {
                return NotFound();
            }

            // Check if user owns this trip
            if (trip.user_id != userId)
            {
                return Forbid();
            }

            return View(trip);
        }

        // =========================================================
        // MY TRIPS
        // =========================================================
        public async Task<IActionResult> MyTrips()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var trips = await _context.trips
                .Include(t => t.Destination)
                .Where(t => t.user_id == userId)
                .OrderByDescending(t => t.created_at)
                .ToListAsync();

            return View(trips);
        }

        // =========================================================
        // DELETE TRIP
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var trip = await _context.trips.FindAsync(id);

            if (trip != null && trip.user_id == userId)
            {
                _context.trips.Remove(trip);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Trip deleted successfully";
            }

            return RedirectToAction("MyTrips");
        }

        // =========================================================
        // AUTOMATED EMAIL & PDF PACKAGE DISPATCH ENGINE
        // =========================================================
        [HttpPost]
        public IActionResult SendPackagePdf(string customerEmail, string customerName, string packageName, string price, string description)
        {
            if (string.IsNullOrEmpty(customerEmail))
            {
                return Json(new { success = false, message = "Recipient email context is missing." });
            }

            try
            {
                byte[] pdfBytes;

                // 1. PDF Generation Block (iTextSharp)
                using (MemoryStream ms = new MemoryStream())
                {
                    Document document = new Document(PageSize.A4, 36, 36, 54, 54);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);

                    document.Open();

                    // Safe Custom RGB Colors to prevent property crashes
                    BaseColor brandPurple = new BaseColor(118, 75, 162);
                    BaseColor charcoal = new BaseColor(44, 62, 80);
                    BaseColor safeBlack = new BaseColor(0, 0, 0);
                    BaseColor safeGray = new BaseColor(128, 128, 128);
                    BaseColor safeDarkGray = new BaseColor(64, 64, 64);

                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 24, brandPurple);
                    Font sectionHeading = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13, charcoal);
                    Font regularBody = FontFactory.GetFont(FontFactory.HELVETICA, 11, safeBlack);
                    Font priceFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, brandPurple);

                    // Fixed Italic Bug by using integer constant style flag
                    Font mutedFooter = FontFactory.GetFont(FontFactory.HELVETICA, 9, Font.ITALIC, safeGray);
                    Font guidelineHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, safeDarkGray);

                    // Document Elements
                    document.Add(new Paragraph("TravelMate AI — Booking Itinerary", titleFont));
                    document.Add(new Paragraph($"Invoice Ref: TM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}", mutedFooter));
                    document.Add(new Paragraph($"Issued On: {DateTime.Now.ToString("dd MMM yyyy, hh:mm tt")}", mutedFooter));
                    document.Add(new Paragraph("\n" + new string('_', 85) + "\n\n"));

                    // Passenger Layout
                    document.Add(new Paragraph("PASSENGER DETAILS", sectionHeading));
                    document.Add(new Paragraph($"Lead Traveler Name: {customerName}", regularBody));
                    document.Add(new Paragraph($"Notification Target Email: {customerEmail}\n\n", regularBody));

                    // Tour Specifics Layout
                    document.Add(new Paragraph("RESERVATION SUMMARY", sectionHeading));
                    document.Add(new Paragraph($"Selected Package: {packageName}", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                    document.Add(new Paragraph($"Total Amount Charged: {price}", priceFont));
                    document.Add(new Paragraph("\nItinerary Structure Overview:", regularBody));
                    document.Add(new Paragraph(description, regularBody));

                    // Policy Layout
                    document.Add(new Paragraph("\n\nImportant Guidelines:", guidelineHeader));
                    document.Add(new Paragraph("* Please preserve a downloaded copy of this digital itinerary during transits.\n* Dynamic updates via AI platform can be synced tracking the unique reference key.", mutedFooter));

                    document.Add(new Paragraph("\n" + new string('_', 85) + "\n\n"));
                    document.Add(new Paragraph("Thank you for choosing TravelMate AI! Have a safe and breathtaking trip. ✨", regularBody));

                    document.Close();
                    pdfBytes = ms.ToArray();
                }

                // 2. Email Setup (MailKit)
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("TravelMate AI", "yourmail@gmail.com")); // Apni verified Gmail ID lagayein
                emailMessage.To.Add(new MailboxAddress(customerName, customerEmail));
                emailMessage.Subject = $"✈️ Booking Confirmed: Itinerary for {packageName}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 25px; text-align: center;'>
                                <h1 style='color: white; margin: 0; font-size: 24px;'>Pack Your Bags! 🎒</h1>
                            </div>
                            <div style='padding: 20px; background-color: #ffffff; color: #333;'>
                                <p style='font-size: 16px;'>Hi <b>{customerName}</b>,</p>
                                <p style='line-height: 1.6;'>Your travel reservation is confirmed! We have generated and attached your official custom e-ticket and itinerary details blueprint as a PDF document directly with this message.</p>
                                
                                <div style='background-color: #f7fafc; padding: 15px; border-left: 4px solid #764ba2; margin: 20px 0;'>
                                    <p style='margin: 4px 0;'><b>Package Plan:</b> {packageName}</p>
                                    <p style='margin: 4px 0;'><b>Invoice Total:</b> {price}</p>
                                </div>
                                <p style='font-size: 13px; color: #718096;'>Safe travels! If you have any inquiries, our support engine is always at your service.</p>
                            </div>
                        </div>"
                };


                bodyBuilder.Attachments.Add($"{packageName.Replace(" ", "_")}_Itinerary.pdf", pdfBytes, ContentType.Parse("application/pdf"));
                emailMessage.Body = bodyBuilder.ToMessageBody();

                // 3. SMTP Handshake & Dispatch
                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.Connect("smtp.gmail.com", 587, false);
                    smtpClient.Authenticate("yourmail@gmail.com", "app password"); // Apna 16-digits ka secure app password lagayein

                    smtpClient.Send(emailMessage);
                    smtpClient.Disconnect(true);
                }

                return Json(new { success = true, message = "Itinerary dispatched cleanly." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}