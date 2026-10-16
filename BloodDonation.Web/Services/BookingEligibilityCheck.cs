using BloodDonation.Domain;
using BloodDonation.Domain.Models;
using BloodDonation.Web.Areas.Donor.Models;
using BloodDonation.Web.Data;

namespace BloodDonation.Web.Services;

// Applies every eligibility rule to a booking (FR3). Both donor booking pages use it,
// so no booking path can skip the rules (#1).
public class BookingEligibilityCheck
{
    private readonly EligibilityService _eligibilityService;
    private readonly StandDownRegister _standDowns;

    public BookingEligibilityCheck(EligibilityService eligibilityService, StandDownRegister standDowns)
    {
        _eligibilityService = eligibilityService;
        _standDowns = standDowns;
    }

    // Builds the stand-down the donor entered on the form, or null if they entered none.
    public static StandDownEvent? StandDownFromForm(
        bool hasStandDownEvent, StandDownReason? reason, DateTime? eventDate, int? durationDays)
    {
        if (!hasStandDownEvent || !reason.HasValue || !eventDate.HasValue || !durationDays.HasValue)
        {
            return null;
        }

        return new StandDownEvent
        {
            Reason = reason.Value,
            EventDate = eventDate.Value,
            DurationDays = durationDays.Value
        };
    }

    // Returns null when the donor can donate on donationDate, otherwise the first rule that fails.
    // A new stand-down is recorded against the donor before checking, so it also blocks later bookings (#4).
    public BookingIneligibleModel? Check(
        string donorUserId,
        DateTime dateOfBirth,
        double weightKg,
        DateTime? lastDonationDate,
        StandDownEvent? newStandDown,
        DateTime donationDate)
    {
        if (newStandDown is not null)
        {
            _standDowns.Record(donorUserId, newStandDown);
        }

        var standDownEvent = _standDowns.For(donorUserId)
            .OrderByDescending(_eligibilityService.StandDownEndDate)
            .FirstOrDefault();

        switch (_eligibilityService.CheckAge(dateOfBirth, lastDonationDate, donationDate))
        {
            case AgeEligibility.TooYoung:
                return new BookingIneligibleModel
                {
                    FailedRule = "Age",
                    Message = $"Donors must be at least {EligibilityRules.MinimumAge} years old.",
                    EarliestEligibleDate = _eligibilityService.MinimumAgeDate(dateOfBirth)
                };
            case AgeEligibility.TooOld:
                return new BookingIneligibleModel
                {
                    FailedRule = "Age",
                    Message = lastDonationDate is null
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

        if (!_eligibilityService.IsWeightEligible(weightKg))
        {
            return new BookingIneligibleModel
            {
                FailedRule = "Weight",
                Message = $"Minimum weight for donation is {EligibilityRules.MinimumWeightKg}kg."
            };
        }

        if (!_eligibilityService.IsIntervalEligible(lastDonationDate, donationDate))
        {
            return new BookingIneligibleModel
            {
                FailedRule = "Interval",
                Message = $"At least {EligibilityRules.MinimumDonationIntervalDays} days must pass between donations.",
                EarliestEligibleDate = _eligibilityService.EarliestDonationDate(lastDonationDate!.Value)
            };
        }

        if (!_eligibilityService.IsStandDownCleared(standDownEvent, donationDate))
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
