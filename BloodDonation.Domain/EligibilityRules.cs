namespace BloodDonation.Domain;

// NZBS eligibility thresholds:
// https://www.nzblood.co.nz/become-a-donor/am-i-eligible/detailed-eligibility-criteria
// https://www.nzblood.co.nz/become-a-donor/am-i-eligible/recent-changes-to-donor-criteria
public static class EligibilityRules
{
    public const int MinimumDonationIntervalDays = 84;
    public const double MinimumWeightKg = 50;
    public const int MinimumAge = 16;

    // Age limits are birthdays: a donor is eligible until the day before this birthday.
    // First-time donors: until their 71st birthday (Transfusion Medicine Handbook 2.2).
    public const int NewDonorAgeLimit = 71;

    // Existing donors (donated in the last 2 years): until their 75th birthday,
    // then until their 81st birthday only with NZBS Medical Officer authorisation.
    public const int ExistingDonorAgeLimit = 75;
    public const int AuthorisedDonorAgeLimit = 81;

    // Lapsed donors (last donation more than 2 years ago) need authorisation from their 71st birthday.
    public const int LapsedDonorAgeLimit = 71;
    public const int LapsedAfterYears = 2;
}
