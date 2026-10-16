using BloodDonation.Domain.Models;
using static BloodDonation.Domain.EligibilityRules;

namespace BloodDonation.Domain;

public class EligibilityService
{
    // Returns false if fewer than 84 days have passed between lastDonationDate and bookingDate.
    // A first-time donor (no last donation) has no interval to wait (TC09).
    public bool IsIntervalEligible(DateTime? lastDonationDate, DateTime bookingDate)
    {
        if (lastDonationDate is null)
        {
            return true;
        }

        var interval = bookingDate - lastDonationDate.Value;
        return interval.TotalDays >= MinimumDonationIntervalDays;
    }

    // NZBS age rules (FR3). No last donation means a first-time donor.
    // Nobody can donate from their 81st birthday, with or without authorisation.
    public AgeEligibility CheckAge(DateTime dateOfBirth, DateTime? lastDonationDate, DateTime bookingDate)
    {
        var age = CalculateAge(dateOfBirth, bookingDate);

        if (age < MinimumAge)
        {
            return AgeEligibility.TooYoung;
        }

        if (lastDonationDate is null)
        {
            return age < NewDonorAgeLimit ? AgeEligibility.Eligible : AgeEligibility.TooOld;
        }

        if (age >= AuthorisedDonorAgeLimit)
        {
            return AgeEligibility.TooOld;
        }

        var ageLimit = IsLapsed(lastDonationDate.Value, bookingDate) ? LapsedDonorAgeLimit : ExistingDonorAgeLimit;
        return age < ageLimit ? AgeEligibility.Eligible : AgeEligibility.NeedsAuthorisation;
    }

    public bool IsAgeEligible(DateTime dateOfBirth, DateTime? lastDonationDate, DateTime bookingDate)
    {
        return CheckAge(dateOfBirth, lastDonationDate, bookingDate) == AgeEligibility.Eligible;
    }

    // Lapsed: last donation more than 2 years before the booking. Exactly 2 years still counts as existing.
    public bool IsLapsed(DateTime lastDonationDate, DateTime bookingDate)
    {
        return lastDonationDate.Date < bookingDate.Date.AddYears(-LapsedAfterYears);
    }

    // 50kg minimum (FR4)
    public bool IsWeightEligible(double weightKg)
    {
        return weightKg >= MinimumWeightKg;
    }

    // Stand-down rule (FR5)
    public bool IsStandDownCleared(StandDownEvent? standDownEvent, DateTime bookingDate)
    {
        if (standDownEvent == null)
        {
            return true; // No stand-down event, eligible
        }

        return bookingDate >= StandDownEndDate(standDownEvent);
    }

    // Age in whole years on the given date.
    public int CalculateAge(DateTime dateOfBirth, DateTime onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > onDate.AddYears(-age)) age--;
        return age;
    }

    // Earliest eligible dates for the time-based rules (FR3).
    public DateTime EarliestDonationDate(DateTime lastDonationDate)
    {
        return lastDonationDate.AddDays(MinimumDonationIntervalDays);
    }

    public DateTime MinimumAgeDate(DateTime dateOfBirth)
    {
        return dateOfBirth.AddYears(MinimumAge);
    }

    public DateTime StandDownEndDate(StandDownEvent standDownEvent)
    {
        return standDownEvent.EventDate.AddDays(standDownEvent.DurationDays);
    }
}
