using BloodDonation.Domain;
using BloodDonation.Domain.Models;

namespace BloodDonation.Tests;

[TestClass]
public class EligibilityServiceTests
{
    private readonly EligibilityService _service = new();

    #region IsIntervalEligible Tests

    [TestMethod]
    public void IsIntervalEligible_83DaysInterval_ReturnsFalse()
    {
        // Arrange
        var lastDonationDate = new DateTime(2024, 1, 1);
        var bookingDate = lastDonationDate.AddDays(83);

        // Act
        var result = _service.IsIntervalEligible(lastDonationDate, bookingDate);

        // Assert
        Assert.IsFalse(result, "83 days should be ineligible (below 84-day minimum)");
    }

    [TestMethod]
    public void IsIntervalEligible_84DaysInterval_ReturnsTrue()
    {
        // Arrange
        var lastDonationDate = new DateTime(2024, 1, 1);
        var bookingDate = lastDonationDate.AddDays(84);

        // Act
        var result = _service.IsIntervalEligible(lastDonationDate, bookingDate);

        // Assert
        Assert.IsTrue(result, "84 days should be eligible (exactly meets 84-day minimum)");
    }

    [TestMethod]
    public void IsIntervalEligible_85DaysInterval_ReturnsTrue()
    {
        // Arrange
        var lastDonationDate = new DateTime(2024, 1, 1);
        var bookingDate = lastDonationDate.AddDays(85);

        // Act
        var result = _service.IsIntervalEligible(lastDonationDate, bookingDate);

        // Assert
        Assert.IsTrue(result, "85 days should be eligible (above 84-day minimum)");
    }

    // TC09: a first-time donor has no previous donation, so the interval rule is skipped.
    [TestMethod]
    public void TC09_IsIntervalEligible_NoPreviousDonation_ReturnsTrue()
    {
        // Arrange
        var bookingDate = new DateTime(2024, 6, 15);

        // Act
        var result = _service.IsIntervalEligible(null, bookingDate);

        // Assert
        Assert.IsTrue(result, "A first-time donor should pass the interval rule");
    }

    [TestMethod]
    public void EarliestDonationDate_Is84DaysAfterLastDonation_AndIsTheFirstEligibleDay()
    {
        // Arrange
        var lastDonationDate = new DateTime(2024, 1, 1);

        // Act
        var earliest = _service.EarliestDonationDate(lastDonationDate);

        // Assert
        Assert.AreEqual(new DateTime(2024, 3, 25), earliest);
        Assert.IsTrue(_service.IsIntervalEligible(lastDonationDate, earliest), "The earliest date should be eligible");
        Assert.IsFalse(_service.IsIntervalEligible(lastDonationDate, earliest.AddDays(-1)), "The day before should not be");
    }

    #endregion

    #region CheckAge Tests - First-time Donors

    // Booking date used by every age test. First-time donors have no last donation.
    private static readonly DateTime AgeBookingDate = new(2024, 6, 15);

    // Existing donors donated in the last 2 years; lapsed donors more than 2 years ago.
    private static readonly DateTime ExistingDonation = AgeBookingDate.AddDays(-100);
    private static readonly DateTime LapsedDonation = AgeBookingDate.AddYears(-3);

    [TestMethod]
    public void CheckAge_FirstTimeDonor_Age15_IsTooYoung()
    {
        // Arrange
        var dateOfBirth = new DateTime(2009, 6, 15); // Exactly 15 on booking date

        // Act
        var result = _service.CheckAge(dateOfBirth, null, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.TooYoung, result, "Age 15 is below the 16-year minimum");
    }

    [TestMethod]
    public void CheckAge_FirstTimeDonor_Age16_IsEligible()
    {
        // Arrange
        var dateOfBirth = new DateTime(2008, 6, 15); // Exactly 16

        // Act
        var result = _service.CheckAge(dateOfBirth, null, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.Eligible, result, "Age 16 exactly meets the 16-year minimum");
    }

    [TestMethod]
    public void CheckAge_FirstTimeDonor_Age17_IsEligible()
    {
        // Arrange
        var dateOfBirth = new DateTime(2007, 6, 15); // 17

        // Act
        var result = _service.CheckAge(dateOfBirth, null, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.Eligible, result, "Age 17 should be eligible for first-time donors");
    }

    [TestMethod]
    public void CheckAge_FirstTimeDonor_DayBefore71stBirthday_IsEligible()
    {
        // Arrange
        var dateOfBirth = new DateTime(1953, 6, 16); // Turns 71 the day after the booking

        // Act
        var result = _service.CheckAge(dateOfBirth, null, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.Eligible, result, "First-time donors are eligible until their 71st birthday");
    }

    // TC07a: NZBS accepts first-time donors until their 71st birthday (#25).
    [TestMethod]
    public void TC07a_CheckAge_FirstTimeDonor_Age71_IsTooOld()
    {
        // Arrange
        var dateOfBirth = new DateTime(1953, 6, 15); // Exactly 71

        // Act
        var result = _service.CheckAge(dateOfBirth, null, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.TooOld, result, "First-time donors are not accepted from their 71st birthday");
    }

    [TestMethod]
    public void CheckAge_FirstTimeDonor_Age72_IsTooOld()
    {
        // Arrange
        var dateOfBirth = new DateTime(1952, 6, 15); // 72

        // Act
        var result = _service.CheckAge(dateOfBirth, null, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.TooOld, result, "Age 72 is over the first-time donor limit");
    }

    #endregion

    #region CheckAge Tests - Existing Donors

    [TestMethod]
    public void CheckAge_ExistingDonor_Age15_IsTooYoung()
    {
        // Arrange
        var dateOfBirth = new DateTime(2009, 6, 15); // Exactly 15

        // Act
        var result = _service.CheckAge(dateOfBirth, ExistingDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.TooYoung, result, "Age 15 is below the 16-year minimum");
    }

    [TestMethod]
    public void CheckAge_ExistingDonor_Age16_IsEligible()
    {
        // Arrange
        var dateOfBirth = new DateTime(2008, 6, 15); // Exactly 16

        // Act
        var result = _service.CheckAge(dateOfBirth, ExistingDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.Eligible, result, "Age 16 exactly meets the 16-year minimum");
    }

    [TestMethod]
    public void CheckAge_ExistingDonor_Age72_IsEligible()
    {
        // Arrange
        var dateOfBirth = new DateTime(1952, 6, 15); // 72

        // Act
        var result = _service.CheckAge(dateOfBirth, ExistingDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.Eligible, result, "Existing donors are eligible past 71, unlike first-time donors");
    }

    [TestMethod]
    public void CheckAge_ExistingDonor_DayBefore75thBirthday_IsEligible()
    {
        // Arrange
        var dateOfBirth = new DateTime(1949, 6, 16); // Turns 75 the day after the booking

        // Act
        var result = _service.CheckAge(dateOfBirth, ExistingDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.Eligible, result, "Existing donors are eligible until their 75th birthday");
    }

    // TC07b: from their 75th birthday, existing donors need NZBS Medical Officer authorisation (#25).
    [TestMethod]
    public void TC07b_CheckAge_ExistingDonor_Age75_NeedsAuthorisation()
    {
        // Arrange
        var dateOfBirth = new DateTime(1949, 6, 15); // Exactly 75

        // Act
        var result = _service.CheckAge(dateOfBirth, ExistingDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.NeedsAuthorisation, result, "Existing donors need authorisation from their 75th birthday");
    }

    [TestMethod]
    public void CheckAge_ExistingDonor_DayBefore81stBirthday_NeedsAuthorisation()
    {
        // Arrange
        var dateOfBirth = new DateTime(1943, 6, 16); // Turns 81 the day after the booking

        // Act
        var result = _service.CheckAge(dateOfBirth, ExistingDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.NeedsAuthorisation, result, "Authorisation applies until the 81st birthday");
    }

    [TestMethod]
    public void CheckAge_ExistingDonor_Age81_IsTooOld()
    {
        // Arrange
        var dateOfBirth = new DateTime(1943, 6, 15); // Exactly 81

        // Act
        var result = _service.CheckAge(dateOfBirth, ExistingDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.TooOld, result, "Nobody can donate from their 81st birthday");
    }

    [TestMethod]
    public void CheckAge_ExistingDonor_Age100_IsTooOld()
    {
        // Arrange
        var dateOfBirth = new DateTime(1924, 6, 15); // 100

        // Act
        var result = _service.CheckAge(dateOfBirth, ExistingDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.TooOld, result, "Age 100 is past the 81st birthday");
    }

    #endregion

    #region CheckAge Tests - Lapsed Donors

    [TestMethod]
    public void CheckAge_LapsedDonor_DayBefore71stBirthday_IsEligible()
    {
        // Arrange
        var dateOfBirth = new DateTime(1953, 6, 16); // Turns 71 the day after the booking

        // Act
        var result = _service.CheckAge(dateOfBirth, LapsedDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.Eligible, result, "Lapsed donors are eligible until their 71st birthday");
    }

    [TestMethod]
    public void CheckAge_LapsedDonor_Age71_NeedsAuthorisation()
    {
        // Arrange
        var dateOfBirth = new DateTime(1953, 6, 15); // Exactly 71

        // Act
        var result = _service.CheckAge(dateOfBirth, LapsedDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.NeedsAuthorisation, result, "Lapsed donors need authorisation from their 71st birthday");
    }

    [TestMethod]
    public void CheckAge_LapsedDonor_Age81_IsTooOld()
    {
        // Arrange
        var dateOfBirth = new DateTime(1943, 6, 15); // Exactly 81

        // Act
        var result = _service.CheckAge(dateOfBirth, LapsedDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.TooOld, result, "Nobody can donate from their 81st birthday");
    }

    [TestMethod]
    public void CheckAge_Age72_DonatedExactlyTwoYearsAgo_IsEligible()
    {
        // Arrange
        var dateOfBirth = new DateTime(1952, 6, 15); // 72
        var lastDonation = AgeBookingDate.AddYears(-2); // Still within the last 2 years

        // Act
        var result = _service.CheckAge(dateOfBirth, lastDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.Eligible, result, "A donation exactly 2 years ago makes the donor existing, not lapsed");
    }

    [TestMethod]
    public void CheckAge_Age72_DonatedTwoYearsAndOneDayAgo_NeedsAuthorisation()
    {
        // Arrange
        var dateOfBirth = new DateTime(1952, 6, 15); // 72
        var lastDonation = AgeBookingDate.AddYears(-2).AddDays(-1); // Lapsed

        // Act
        var result = _service.CheckAge(dateOfBirth, lastDonation, AgeBookingDate);

        // Assert
        Assert.AreEqual(AgeEligibility.NeedsAuthorisation, result, "One day past 2 years makes the donor lapsed");
    }

    [TestMethod]
    public void IsAgeEligible_IsTrueOnlyWhenCheckAgeIsEligible()
    {
        Assert.IsTrue(_service.IsAgeEligible(new DateTime(1953, 6, 16), null, AgeBookingDate));
        Assert.IsFalse(_service.IsAgeEligible(new DateTime(1949, 6, 15), ExistingDonation, AgeBookingDate), "Needs authorisation is not eligible to book");
        Assert.IsFalse(_service.IsAgeEligible(new DateTime(2009, 6, 15), null, AgeBookingDate));
    }

    [TestMethod]
    public void CalculateAge_DayBeforeBirthday_ReturnsPreviousAge()
    {
        var dateOfBirth = new DateTime(2008, 6, 15);

        Assert.AreEqual(15, _service.CalculateAge(dateOfBirth, new DateTime(2024, 6, 14)));
        Assert.AreEqual(16, _service.CalculateAge(dateOfBirth, new DateTime(2024, 6, 15)));
    }

    [TestMethod]
    public void MinimumAgeDate_Is16thBirthday_AndIsTheFirstEligibleDay()
    {
        // Arrange
        var dateOfBirth = new DateTime(2008, 6, 15);

        // Act
        var earliest = _service.MinimumAgeDate(dateOfBirth);

        // Assert
        Assert.AreEqual(new DateTime(2024, 6, 15), earliest);
        Assert.IsTrue(_service.IsAgeEligible(dateOfBirth, null, earliest), "The 16th birthday should be eligible");
        Assert.IsFalse(_service.IsAgeEligible(dateOfBirth, null, earliest.AddDays(-1)), "The day before should not be");
    }

    #endregion

    #region IsWeightEligible Tests

    [TestMethod]
    public void IsWeightEligible_49Kg_ReturnsFalse()
    {
        // Arrange
        var weight = 49.0;

        // Act
        var result = _service.IsWeightEligible(weight);

        // Assert
        Assert.IsFalse(result, "49 kg should be ineligible (below 50 kg minimum)");
    }

    [TestMethod]
    public void IsWeightEligible_50Kg_ReturnsTrue()
    {
        // Arrange
        var weight = 50.0;

        // Act
        var result = _service.IsWeightEligible(weight);

        // Assert
        Assert.IsTrue(result, "50 kg should be eligible (exactly meets 50 kg minimum)");
    }

    [TestMethod]
    public void IsWeightEligible_51Kg_ReturnsTrue()
    {
        // Arrange
        var weight = 51.0;

        // Act
        var result = _service.IsWeightEligible(weight);

        // Assert
        Assert.IsTrue(result, "51 kg should be eligible (above 50 kg minimum)");
    }

    [TestMethod]
    public void IsWeightEligible_49_9Kg_ReturnsFalse()
    {
        // Arrange
        var weight = 49.9;

        // Act
        var result = _service.IsWeightEligible(weight);

        // Assert
        Assert.IsFalse(result, "49.9 kg should be ineligible (below 50 kg minimum)");
    }

    [TestMethod]
    public void IsWeightEligible_50_1Kg_ReturnsTrue()
    {
        // Arrange
        var weight = 50.1;

        // Act
        var result = _service.IsWeightEligible(weight);

        // Assert
        Assert.IsTrue(result, "50.1 kg should be eligible (above 50 kg minimum)");
    }

    #endregion

    #region IsStandDownCleared Tests

    [TestMethod]
    public void IsStandDownCleared_NoStandDownEvent_ReturnsTrue()
    {
        // Arrange
        var bookingDate = new DateTime(2024, 6, 15);

        // Act
        var result = _service.IsStandDownCleared(null, bookingDate);

        // Assert
        Assert.IsTrue(result, "No stand-down event should be eligible");
    }

    [TestMethod]
    public void IsStandDownCleared_OneDayBeforeStandDownEnds_ReturnsFalse()
    {
        // Arrange
        var eventDate = new DateTime(2024, 1, 1);
        var durationDays = 14;
        var standDownEvent = new StandDownEvent
        {
            EventDate = eventDate,
            DurationDays = durationDays
        };
        var bookingDate = eventDate.AddDays(durationDays - 1); // One day before stand-down ends

        // Act
        var result = _service.IsStandDownCleared(standDownEvent, bookingDate);

        // Assert
        Assert.IsFalse(result, "Booking before stand-down ends should be ineligible");
    }

    [TestMethod]
    public void IsStandDownCleared_OnStandDownEndDate_ReturnsTrue()
    {
        // Arrange
        var eventDate = new DateTime(2024, 1, 1);
        var durationDays = 14;
        var standDownEvent = new StandDownEvent
        {
            EventDate = eventDate,
            DurationDays = durationDays
        };
        var bookingDate = eventDate.AddDays(durationDays); // Exactly when stand-down ends

        // Act
        var result = _service.IsStandDownCleared(standDownEvent, bookingDate);

        // Assert
        Assert.IsTrue(result, "Booking on stand-down end date should be eligible");
    }

    [TestMethod]
    public void IsStandDownCleared_OneDayAfterStandDownEnds_ReturnsTrue()
    {
        // Arrange
        var eventDate = new DateTime(2024, 1, 1);
        var durationDays = 14;
        var standDownEvent = new StandDownEvent
        {
            EventDate = eventDate,
            DurationDays = durationDays
        };
        var bookingDate = eventDate.AddDays(durationDays + 1); // One day after stand-down ends

        // Act
        var result = _service.IsStandDownCleared(standDownEvent, bookingDate);

        // Assert
        Assert.IsTrue(result, "Booking after stand-down ends should be eligible");
    }

    [TestMethod]
    public void IsStandDownCleared_DuringStandDown_ReturnsFalse()
    {
        // Arrange
        var eventDate = new DateTime(2024, 1, 1);
        var durationDays = 14;
        var standDownEvent = new StandDownEvent
        {
            EventDate = eventDate,
            DurationDays = durationDays
        };
        var bookingDate = eventDate.AddDays(7); // Middle of stand-down period

        // Act
        var result = _service.IsStandDownCleared(standDownEvent, bookingDate);

        // Assert
        Assert.IsFalse(result, "Booking during stand-down period should be ineligible");
    }

    [TestMethod]
    public void StandDownEndDate_IsEventDatePlusDuration_AndIsTheFirstEligibleDay()
    {
        // Arrange
        var standDownEvent = new StandDownEvent
        {
            EventDate = new DateTime(2024, 1, 1),
            DurationDays = 14
        };

        // Act
        var end = _service.StandDownEndDate(standDownEvent);

        // Assert
        Assert.AreEqual(new DateTime(2024, 1, 15), end);
        Assert.IsTrue(_service.IsStandDownCleared(standDownEvent, end), "The end date should be eligible");
        Assert.IsFalse(_service.IsStandDownCleared(standDownEvent, end.AddDays(-1)), "The day before should not be");
    }

    #endregion
}
