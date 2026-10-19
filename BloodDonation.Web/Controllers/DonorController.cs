using System.Security.Claims;
using BloodDonation.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Web.Data;
using BloodDonation.Web.Models;
using BloodDonation.Web.Services;

namespace BloodDonation.Web.Controllers
{
    // Only donors can access this controller
    [Authorize(Roles = Roles.Donor)]
    public class DonorController : Controller
    {
        private readonly BookingEligibilityCheck _eligibilityCheck;

        // Gets the service we use to check whether the donor can donate
        public DonorController(BookingEligibilityCheck eligibilityCheck)
        {
            _eligibilityCheck = eligibilityCheck;
        }

        // Gets the account ID of the donor who is currently signed in
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public IActionResult Dashboard()
        {
            // Only the signed-in donor's own booking (D3 / NFR3).
            var booking = InMemoryStore.Bookings.LastOrDefault(b => b.DonorUserId == CurrentUserId);
            DonationSession? session = null;

            // Finds the session details if the donor has a booking
            if (booking != null)
                session = InMemoryStore.Sessions.FirstOrDefault(s => s.Id == booking.SessionId);

            // Passes the booking and session details to the dashboard
            ViewBag.Booking = booking;
            ViewBag.Session = session;
            return View();
        }

        // Opens the booking page with sessions ordered by date
        [HttpGet]
        public IActionResult Book()
        {
            ViewBag.Sessions = InMemoryStore.Sessions.OrderBy(s => s.SessionDate).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Book(BookingViewModel model)
        {
            // Reject the form when required fields fail server-side validation.
            if (!ModelState.IsValid)
            {
                // Loads the sessions again so the form can still display them
                ViewBag.Sessions = InMemoryStore.Sessions
                    .OrderBy(s => s.SessionDate)
                    .ToList();

                return View(model);
            }

            // Finds the session the donor selected
            var session = InMemoryStore.Sessions.FirstOrDefault(s => s.Id == model.SessionId);

            // Sends them back to choose another session if this one is missing or full
            if (session == null || session.SpotsLeft <= 0)
            {
                TempData["Error"] = "That session is no longer available. Please choose another.";
                return RedirectToAction("Book");
            }

            // Every eligibility rule, judged on the day the donor would donate (#1, #4).
            var standDownEvent = BookingEligibilityCheck.StandDownFromForm(
                model.HasStandDownEvent, model.StandDownReason, model.StandDownEventDate, model.StandDownDurationDays);

            // Checks the donor's details and any stand-down against the session date
            var ineligible = _eligibilityCheck.Check(
                CurrentUserId!,
                model.DateOfBirth!.Value,
                model.WeightKg!.Value,
                model.LastDonationDate,
                standDownEvent,
                session.SessionDate);

            // Explains why they cannot donate and includes the earliest eligible date if available
            if (ineligible != null)
            {
                TempData["Error"] = ineligible.EarliestEligibleDate.HasValue
                    ? $"You can't donate on {session.SessionDate:dddd d MMM yyyy}. {ineligible.Message} " +
                      $"You can book a session from {ineligible.EarliestEligibleDate.Value:d MMM yyyy}."
                    : $"You can't donate on {session.SessionDate:dddd d MMM yyyy}. {ineligible.Message}";
                return RedirectToAction("Book");
            }

            // Creates the booking using the donor's account ID and form details
            var booking = new Booking
            {
                DonorUserId = CurrentUserId,
                DonorName = model.DonorName,
                DonorEmail = model.DonorEmail,
                SessionId = model.SessionId,

                // Blood type is optional, so store an empty string when none is supplied.
                BloodType = model.BloodType ?? "",

                Notes = model.Notes
            };

            // Saves the booking and increases the session's booked count
            InMemoryStore.Bookings.Add(booking);
            session.BookedCount++;

            // Shows the appointment confirmation on the dashboard
            TempData["Message"] =
                $"Your appointment on {session.SessionDate:dddd d MMM yyyy} at {session.Location} is confirmed.";

            return RedirectToAction("Dashboard");
        }

        // Finds the booking the donor wants to cancel
        [HttpPost]
        public IActionResult Cancel(string BookingId)
        {
            var booking = InMemoryStore.Bookings.FirstOrDefault(b => b.Id == BookingId);
            if (booking != null)
            {
                // Stops donors from cancelling someone else's booking
                if (booking.DonorUserId != CurrentUserId)
                    return Forbid();

                RemoveBooking(booking);
                TempData["Message"] = "Your appointment has been cancelled.";
            }

            return RedirectToAction("Dashboard");
        }

        // Removes the existing booking before the donor chooses a new session
        [HttpPost]
        public IActionResult Reschedule(string BookingId)
        {
            var booking = InMemoryStore.Bookings.FirstOrDefault(b => b.Id == BookingId);
            if (booking != null)
            {
                // Stops donors from rescheduling someone else's booking
                if (booking.DonorUserId != CurrentUserId)
                    return Forbid();

                RemoveBooking(booking);
            }

            TempData["Message"] = "Your appointment was cancelled — choose a new session below.";
            return RedirectToAction("Book");
        }

        // Releases the slot and removes the booking.
        private static void RemoveBooking(Booking booking)
        {
            // Reduces the booked count if the session still exists
            var session = InMemoryStore.Sessions.FirstOrDefault(s => s.Id == booking.SessionId);
            if (session != null) session.BookedCount--;

            InMemoryStore.Bookings.Remove(booking);
        }
    }
}