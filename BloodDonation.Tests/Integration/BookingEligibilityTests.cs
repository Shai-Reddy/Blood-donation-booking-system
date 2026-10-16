using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using BloodDonation.Domain;
using BloodDonation.Web.Data;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

[TestClass]
[DoNotParallelize]
public class BookingEligibilityTests
{
    private const string Password = "Eligibility#Test1";
    private const string SessionId = "S3";

    private static WebApplicationFactory<Program> _factory = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>();
        _ = _factory.Server;
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory.Dispose();
    }

    [TestMethod]
    public async Task DonorWithLastDonationBeforeMinimumInterval_CannotBook()
    {
        var client = await RegisteredDonorAsync();

        await PostBookingAsync(
            client,
            lastDonationDate: InMemoryStore.Sessions.Single(session => session.Id == SessionId).SessionDate
                .AddDays(-(EligibilityRules.MinimumDonationIntervalDays - 1)));

        Assert.IsNull(await DashboardBookingIdAsync(client), "An ineligible booking should not appear on the dashboard.");
    }

    [TestMethod]
    public async Task DonorAtMinimumDonationInterval_CanBook()
    {
        var client = await RegisteredDonorAsync();

        await PostBookingAsync(
            client,
            lastDonationDate: InMemoryStore.Sessions.Single(session => session.Id == SessionId).SessionDate
                .AddDays(-EligibilityRules.MinimumDonationIntervalDays));

        Assert.IsNotNull(await DashboardBookingIdAsync(client), "An eligible booking should appear on the dashboard.");
    }

    [TestMethod]
    public async Task DonorBelowMinimumWeight_CannotBook()
    {
        var client = await RegisteredDonorAsync();

        await PostBookingAsync(client, weightKg: EligibilityRules.MinimumWeightKg - 0.1);

        Assert.IsNull(await DashboardBookingIdAsync(client), "An ineligible booking should not appear on the dashboard.");
    }

    [TestMethod]
    public async Task DonorBelowMinimumAge_CannotBook()
    {
        var client = await RegisteredDonorAsync();

        await PostBookingAsync(
            client,
            dateOfBirth: DateTime.Today.AddYears(-(EligibilityRules.MinimumAge - 1)));

        Assert.IsNull(await DashboardBookingIdAsync(client), "An ineligible booking should not appear on the dashboard.");
    }

    [TestMethod]
    public async Task DonorWithActiveStandDown_CannotBook()
    {
        var client = await RegisteredDonorAsync();

        await PostBookingAsync(
            client,
            hasStandDownEvent: true,
            standDownReason: "Tattoo",
            standDownEventDate: DateTime.Today,
            standDownDurationDays: EligibilityRules.MinimumDonationIntervalDays);

        Assert.IsNull(await DashboardBookingIdAsync(client), "An ineligible booking should not appear on the dashboard.");
    }

    private static async Task PostBookingAsync(
        HttpClient client,
        DateTime? dateOfBirth = null,
        double? weightKg = null,
        DateTime? lastDonationDate = null,
        bool hasStandDownEvent = false,
        string standDownReason = "",
        DateTime? standDownEventDate = null,
        int? standDownDurationDays = null)
    {
        var response = await IntegrationTestHelpers.PostWithTokenAsync(client, "/Donor/Book", "/Donor/Book",
            ("SessionId", SessionId),
            ("DonorName", "Test Donor"),
            ("DonorEmail", "test.donor@test.local"),
            ("BloodType", ""),
            ("DateOfBirth", FormDate(dateOfBirth ?? DateTime.Today.AddYears(-EligibilityRules.MinimumAge))),
            ("WeightKg", (weightKg ?? EligibilityRules.MinimumWeightKg).ToString(CultureInfo.InvariantCulture)),
            ("LastDonationDate", FormDate(lastDonationDate)),
            ("HasStandDownEvent", hasStandDownEvent ? "true" : "false"),
            ("StandDownReason", standDownReason),
            ("StandDownEventDate", FormDate(standDownEventDate)),
            ("StandDownDurationDays", standDownDurationDays?.ToString(CultureInfo.InvariantCulture) ?? ""));

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, "Booking form submission should redirect.");
    }

    private static string FormDate(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    }

    private static async Task<HttpClient> RegisteredDonorAsync()
    {
        var client = IntegrationTestHelpers.CreateClient(_factory);
        var email = $"donor-{Guid.NewGuid():N}@test.local";

        var response = await IntegrationTestHelpers.PostWithTokenAsync(client, "/Account/Register", "/Account/Register",
            ("FullName", "Test Donor"),
            ("Email", email),
            ("Password", Password),
            ("ConfirmPassword", Password),
            ("DateOfBirth", "1990-01-01"),
            ("WeightKg", EligibilityRules.MinimumWeightKg.ToString(CultureInfo.InvariantCulture)),
            ("TravelledOverseas", "No"));
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, $"Registering {email} failed.");
        return client;
    }

    private static async Task<string?> DashboardBookingIdAsync(HttpClient client)
    {
        var dashboard = await client.GetStringAsync("/Donor/Dashboard");
        var match = Regex.Match(dashboard, "name=\"BookingId\" value=\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : null;
    }
}