using System.Security.Claims;
using BloodDonation.Web.Identity;
using Microsoft.AspNetCore.Authorization;
using BloodDonation.Domain.Models;
using BloodDonation.Web.Areas.Donor.Models;
using BloodDonation.Web.Data;
using BloodDonation.Web.Services;
using Microsoft.AspNetCore.Mvc;
using DonorModel = BloodDonation.Domain.Models.Donor;

namespace BloodDonation.Web.Areas.Donor.Controllers;

[Area("Donor")]
[Authorize(Roles = Roles.Donor)]
public class BookingController : Controller
{
    private readonly BookingEligibilityCheck _eligibilityCheck;
    private readonly SampleDataStore _store;

    public BookingController(BookingEligibilityCheck eligibilityCheck, SampleDataStore store)
    {
        _eligibilityCheck = eligibilityCheck;
        _store = store;
    }

    [HttpGet]
    public IActionResult Book()
    {
        return View(new BookingFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Book(BookingFormModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var bookingDate = DateTime.Today;

        var standDownEvent = BookingEligibilityCheck.StandDownFromForm(
            model.HasStandDownEvent, model.StandDownReason, model.StandDownEventDate, model.StandDownDurationDays);

        var ineligible = _eligibilityCheck.Check(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            model.DateOfBirth,
            model.WeightKg,
            model.LastDonationDate,
            standDownEvent,
            bookingDate);
        if (ineligible is not null)
        {
            return View("Ineligible", ineligible);
        }

        var donor = new DonorModel
        {
            Id = _store.Donors.Count + 1,
            Name = "Guest Donor",
            DateOfBirth = model.DateOfBirth,
            WeightKg = model.WeightKg,
            LastDonationDate = model.LastDonationDate,
            Role = UserRole.Donor
        };
        _store.Donors.Add(donor);

        var appointment = new Appointment
        {
            Id = _store.Appointments.Count + 1,
            DonorId = donor.Id,
            SessionId = 0,
            CreatedDate = DateTime.Now,
            ScheduledDate = bookingDate,
            Status = AppointmentStatus.Booked
        };
        _store.Appointments.Add(appointment);

        return View("Confirmation", appointment);
    }
}
