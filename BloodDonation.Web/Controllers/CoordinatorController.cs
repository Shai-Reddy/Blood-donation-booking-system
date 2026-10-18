using BloodDonation.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BloodDonation.Web.Data;

namespace BloodDonation.Web.Controllers
{
    // Only coordinators can access this controller
    [Authorize(Roles = Roles.Coordinator)]
    public class CoordinatorController : Controller
    {
        // Loads the dashboard with any blood type or status filters selected
        [HttpGet]
        public IActionResult Dashboard(string? bloodType, string? status)
        {
            // Starts with all the bookings before applying the filters
            var bookings = InMemoryStore.Bookings.AsEnumerable();

            // Only keeps bookings with the selected blood type
            if (!string.IsNullOrEmpty(bloodType))
                bookings = bookings.Where(b => b.BloodType == bloodType);

            // Only keeps bookings with the selected status
            if (!string.IsNullOrEmpty(status))
                bookings = bookings.Where(b => b.Status == status);

            var filteredBookings = bookings.ToList();

            // Gets the overall totals from all sessions and bookings
            var totalCapacity = InMemoryStore.Sessions.Sum(s => s.Capacity);
            var totalBooked = InMemoryStore.Sessions.Sum(s => s.BookedCount);
            var totalBookings = InMemoryStore.Bookings.Count;
            var deferredBookings = InMemoryStore.Bookings.Where(b => b.Status == "Deferred").ToList();
            var noShowCount = InMemoryStore.Bookings.Count(b => b.Status == "NoShow");

            // Average lead time: days between when the booking was made and the session date.
            // Finds each booking's session and uses the first booking for that session to get the creation date
            var leadTimes = InMemoryStore.Bookings
                .Select(b => InMemoryStore.Sessions.FirstOrDefault(s => s.Id == b.SessionId))
                .Where(s => s != null)
                .Select(s => (s!.SessionDate.Date - InMemoryStore.Bookings.First(b => b.SessionId == s.Id).CreatedAt.Date).TotalDays)
                .ToList();

            // Rounds the average to one decimal place, or uses zero if there are no lead times
            var avgLeadTime = leadTimes.Count > 0 ? Math.Round(leadTimes.Average(), 1) : 0;

            // Groups deferred bookings by reason, with the most common reasons shown first
            var deferralReasons = deferredBookings
                .GroupBy(b => b.DeferralReason ?? "Not specified")
                .Select(g => new { Reason = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            // Passes the overall figures, deferral reasons and stock levels to the dashboard
            ViewBag.TotalCapacity = totalCapacity;
            ViewBag.TotalBooked = totalBooked;
            ViewBag.DeferralRate = totalBookings > 0 ? Math.Round((double)deferredBookings.Count / totalBookings * 100, 1) : 0;
            ViewBag.NoShowRate = totalBookings > 0 ? Math.Round((double)noShowCount / totalBookings * 100, 1) : 0;
            ViewBag.AvgLeadTime = avgLeadTime;
            ViewBag.DeferralReasons = deferralReasons;
            ViewBag.StockLevels = InMemoryStore.StockLevels;
            // Passes through the filtered bookings and keeps track of the selected filters
            ViewBag.FilteredBookings = filteredBookings;
            ViewBag.SelectedBloodType = bloodType;
            ViewBag.SelectedStatus = status;

            return View();
        }
    }
}