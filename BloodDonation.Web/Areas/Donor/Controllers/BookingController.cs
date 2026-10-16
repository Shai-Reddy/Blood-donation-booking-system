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

        var ineligible = Evaluate(model, bookingDate, standDownEvent);
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

    private BookingIneligibleModel? Evaluate(
        BookingFormModel model,
        DateTime bookingDate,
        StandDownEvent? standDownEvent)
    {
        switch (_eligibilityService.CheckAge(model.DateOfBirth, model.LastDonationDate, bookingDate))
        {
            case AgeEligibility.TooYoung:
                return new BookingIneligibleModel
                {
                    FailedRule = "Age",
                    Message = $"Donors must be at least {EligibilityRules.MinimumAge} years old.",
                    EarliestEligibleDate = _eligibilityService.MinimumAgeDate(model.DateOfBirth)
                };
            case AgeEligibility.TooOld:
                return new BookingIneligibleModel
                {
                    FailedRule = "Age",
                    Message = model.LastDonationDate is null
                        ? $"First-time donors must be under {EligibilityRules.NewDonorAgeLimit}."
                        : $"Donors can donate until they turn {EligibilityRules.AuthorisedDonorAgeLimit}."
                };
            case AgeEligibility.NeedsAuthorisation:
                return new BookingIneligibleModel
                {
                    FailedRule = "Age",
                    Message = "At your age, donating needs authorisation from an NZBS Medical Officer. " +
                              "Please contact NZBS before booking."
                };
        }

        if (!_eligibilityService.IsWeightEligible(model.WeightKg))
        {
            return new BookingIneligibleModel
            {
                FailedRule = "Weight",
                Message = $"Minimum weight for donation is {EligibilityRules.MinimumWeightKg}kg."
            };
        }

        if (!_eligibilityService.IsIntervalEligible(model.LastDonationDate, bookingDate))
        {
            return new BookingIneligibleModel
            {
                FailedRule = "Interval",
                Message = $"At least {EligibilityRules.MinimumDonationIntervalDays} days must pass between donations.",
                EarliestEligibleDate = _eligibilityService.EarliestDonationDate(model.LastDonationDate!.Value)
            };
        }

        if (!_eligibilityService.IsStandDownCleared(standDownEvent, bookingDate))
        {
            return new BookingIneligibleModel
            {
                FailedRule = "Stand-down",
                Message = $"Stand-down period for {standDownEvent!.Reason} has not elapsed.",
                EarliestEligibleDate = _eligibilityService.StandDownEndDate(standDownEvent)
            };
        }

        return null;
    }
}
