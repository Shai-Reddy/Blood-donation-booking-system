using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

// #4, FR3, TC08: a stand-down a donor has entered keeps blocking their bookings until it ends,
// even when they leave the stand-down fields empty next time.
// Each test registers its own donor, so one donor's stand-down cannot leak into another test.
// Not parallelised: eligible bookings are added to the shared SampleDataStore, whose lists are not thread-safe.
[TestClass]
[DoNotParallelize]
public class StandDownRecordTests
{
    private const string Password = "StandDown#Test1";
    private const string BookingUrl = "/Donor/Booking/Book";
    private const string Refused = "You're not eligible to book right now";
    private const string Booked = "Booking Confirmed";

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
    public async Task TC08_StandDownEnteredEarlier_StillBlocksBookingWithoutIt()
    {
        var donor = await RegisteredDonorAsync();
        var first = await BookAsync(donor, withActiveStandDown: true);
        StringAssert.Contains(first, Refused, "The stand-down should refuse the first booking.");

        var second = await BookAsync(donor, withActiveStandDown: false);

        StringAssert.Contains(second, Refused, "The recorded stand-down should still refuse the booking.");
    }

    [TestMethod]
    public async Task StandDownEnteredByAnotherDonor_DoesNotBlockThisDonor()
    {
        var other = await RegisteredDonorAsync();
        await BookAsync(other, withActiveStandDown: true);
        var donor = await RegisteredDonorAsync();

        var body = await BookAsync(donor, withActiveStandDown: false);

        StringAssert.Contains(body, Booked);
    }

    private static async Task<string> BookAsync(HttpClient client, bool withActiveStandDown)
    {
        var form = new Dictionary<string, string>
        {
            ["DateOfBirth"] = "1990-01-01",
            ["WeightKg"] = "70",
            ["LastDonationDate"] = "",
            ["HasStandDownEvent"] = withActiveStandDown ? "true" : "false",
            ["StandDownReason"] = withActiveStandDown ? "Tattoo" : "",
            ["StandDownEventDate"] = withActiveStandDown ? DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "",
            ["StandDownDurationDays"] = withActiveStandDown ? "120" : "",
            ["__RequestVerificationToken"] = await TokenAsync(client, BookingUrl),
        };

        var response = await client.PostAsync(BookingUrl, new FormUrlEncodedContent(form));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The booking form should return a result page.");
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<HttpClient> RegisteredDonorAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = $"donor-{Guid.NewGuid():N}@test.local";

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FullName"] = "Test Donor",
            ["Email"] = email,
            ["Password"] = Password,
            ["ConfirmPassword"] = Password,
            ["DateOfBirth"] = "1990-01-01",
            ["WeightKg"] = "70",
            ["TravelledOverseas"] = "No",
            ["__RequestVerificationToken"] = await TokenAsync(client, "/Account/Register"),
        });

        var response = await client.PostAsync("/Account/Register", form);
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, $"Registering {email} failed.");
        return client;
    }

    private static async Task<string> TokenAsync(HttpClient client, string formPageUrl)
    {
        var page = await client.GetStringAsync(formPageUrl);
        return Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
    }
}
