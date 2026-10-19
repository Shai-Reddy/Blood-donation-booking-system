using BloodDonation.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Web.Data;
using BloodDonation.Web.Models;

namespace BloodDonation.Web.Controllers
{
    // Only staff can access this controller
    [Authorize(Roles = Roles.Staff)]
    public class StaffController : Controller
    {
        // Loads the figures we need for the staff dashboard
        [HttpGet]
        public IActionResult Dashboard()
        {
            // Keep the total session count as the total number of sessions.
            ViewBag.TotalSessions = InMemoryStore.Sessions.Count;

            // Count only sessions scheduled for today for the dashboard.
            ViewBag.TodaySessions = SessionStats.CountOn(InMemoryStore.Sessions, DateTime.Today);

            // Counts all bookings, deferred bookings and no-shows
            ViewBag.TotalBookings = InMemoryStore.Bookings.Count;
            ViewBag.FlaggedCount = InMemoryStore.Bookings.Count(b => b.Status == "Deferred");
            ViewBag.NoShowCount = InMemoryStore.Bookings.Count(b => b.Status == "NoShow");
            return View();
        }

        // Gets the bookings and their session details for the staff bookings page
        [HttpGet]
        public IActionResult Bookings()
        {
            var bookings = InMemoryStore.Bookings
                .Select(b => new
                {
                    Booking = b,
                    Session = InMemoryStore.Sessions.FirstOrDefault(s => s.Id == b.SessionId)
                })
                .ToList();

            ViewBag.Bookings = bookings;
            return View();
        }

        // Records the outcome staff have selected for a booking
        [HttpPost]
        public IActionResult RecordOutcome(string bookingId, string outcome, string? deferralReason)
        {
            // Finds the booking we need to update
            var booking = InMemoryStore.Bookings.FirstOrDefault(b => b.Id == bookingId);
            if (booking != null)
            {
                //deferral must have a reason so that staff can record why donor is deferred
                if (outcome == "Deferred" && string.IsNullOrWhiteSpace(deferralReason))
                {
                    ModelState.AddModelError(
                        "DeferralReason",
                        "A reason is required when the donor is deferred.");

                    //rebuild the booking data so forms can display again with validation error
                    ViewBag.Bookings = InMemoryStore.Bookings.Select(b => new
                    {
                        Booking = b,
                        Session = InMemoryStore.Sessions.FirstOrDefault(s => s.Id == b.SessionId)
                    })
                    .ToList();
                    return View("Bookings");
                }

                //update booking after all validation has passed
                booking.Status = outcome;

                if (outcome == "Deferred")
                {
                    //store the given reason without leading whitespace
                    booking.DeferralReason = deferralReason?.Trim();
                }

                // Adds one to the stock if the outcome is donated and the blood type is listed
                if (outcome == "Donated" && !string.IsNullOrEmpty(booking.BloodType) && InMemoryStore.StockLevels.TryGetValue(booking.BloodType, out var currentStock))
                {
                    InMemoryStore.StockLevels[booking.BloodType] = currentStock + 1;
                }
            }

            return RedirectToAction("Bookings");
        }

        // Opens the sessions page with the sessions ordered by date
        [HttpGet]
        public IActionResult Sessions()
        {
            ViewBag.Sessions = InMemoryStore.Sessions.OrderBy(s => s.SessionDate).ToList();
            return View();
        }

        // Handles the form for adding a new donation session
        [HttpPost]
        public IActionResult Sessions(SessionViewModel model)
       {
         // Reject the form when required fields fail server-side validation.
         if (!ModelState.IsValid)
        {
        // Rebuild the session list so it is still available
        // when the form is returned with validation errors.
        ViewBag.Sessions = InMemoryStore.Sessions
            .OrderBy(s => s.SessionDate)
            .ToList();

        return View(model);
    }

    // Add the new session after validation has passed.
    // Gives it an ID based on the session count and starts with no bookings
    InMemoryStore.Sessions.Add(new DonationSession
    {
        Id = "S" + (InMemoryStore.Sessions.Count + 1),
        SessionDate = model.SessionDate,
        Time = model.Time,
        Location = model.Location,
        Capacity = model.Capacity,
        BookedCount = 0
    });

    // Shows a confirmation after redirecting back to the sessions page
    TempData["Message"] = "New session added.";

    return RedirectToAction("Sessions");
        }
    }
}