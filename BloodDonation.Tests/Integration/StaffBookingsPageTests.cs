using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

// #35, FR8: a booking a donor makes appears on the staff bookings page that the staff dashboard links to,
// with a form to record its outcome. Follows the link from the dashboard rather than assuming its URL.
// Not parallelised: the booking is added to the shared InMemoryStore, whose lists are not thread-safe.
[TestClass]
[DoNotParallelize]
public class StaffBookingsPageTests
{
    private const string StaffEmail = "staff@test.local";
    private const string StaffPassword = "Staff#Test1";
    private const string DonorPassword = "StaffPage#Test1";

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
    public async Task DonorBooking_AppearsOnStaffBookingsPage_WithOutcomeForm()
    {
        var donorName = $"Staff Page Donor {Guid.NewGuid():N}";
        var bookingId = await DonorBookingAsync(donorName);
        var staff = await SignedInStaffAsync();

        var dashboard = await staff.GetStringAsync("/Staff/Dashboard");
        var bookingsLink = Regex.Match(dashboard, "href=\"([^\"]+)\"[^>]*>View All Bookings").Groups[1].Value;
        Assert.IsFalse(string.IsNullOrEmpty(bookingsLink), "The staff dashboard should link to the bookings page.");
        var bookingsPage = await staff.GetStringAsync(bookingsLink);

        StringAssert.Contains(bookingsPage, donorName, "The donor's booking should be listed for staff.");
        StringAssert.Contains(bookingsPage, $"name=\"BookingId\" value=\"{bookingId}\"",
            "Staff should have a form to record the outcome of this booking.");
    }

    private static async Task<string> DonorBookingAsync(string donorName)
    {
        var client = NewClient();
        var email = $"donor-{Guid.NewGuid():N}@test.local";

        var registration = await PostWithTokenAsync(client, "/Account/Register", "/Account/Register",
            ("FullName", donorName),
            ("Email", email),
            ("Password", DonorPassword),
            ("ConfirmPassword", DonorPassword),
            ("DateOfBirth", "1990-01-01"),
            ("WeightKg", "70"),
            ("TravelledOverseas", "No"));
        Assert.AreEqual(HttpStatusCode.Redirect, registration.StatusCode, $"Registering {email} failed.");

        // Eligibility fields are sent so the booking succeeds whether or not the booking page checks them (#1).
        var booking = await PostWithTokenAsync(client, "/Donor/Book", "/Donor/Book",
            ("SessionId", "S1"),
            ("DonorName", donorName),
            ("DonorEmail", email),
            ("BloodType", "O+"),
            ("DateOfBirth", "1990-01-01"),
            ("WeightKg", "70"),
            ("LastDonationDate", ""),
            ("HasStandDownEvent", "false"));
        Assert.AreEqual(HttpStatusCode.Redirect, booking.StatusCode, "Booking failed.");

        var dashboard = await client.GetStringAsync("/Donor/Dashboard");
        var match = Regex.Match(dashboard, "name=\"BookingId\" value=\"([^\"]+)\"");
        Assert.IsTrue(match.Success, "The booking should appear on the donor's dashboard.");
        return match.Groups[1].Value;
    }

    private static async Task<HttpClient> SignedInStaffAsync()
    {
        var client = NewClient();
        var response = await PostWithTokenAsync(client, "/Account/StaffLogin", "/Account/StaffLogin",
            ("Email", StaffEmail),
            ("Password", StaffPassword));
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, "Staff login failed.");
        return client;
    }

    private static HttpClient NewClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static async Task<HttpResponseMessage> PostWithTokenAsync(
        HttpClient client, string formPageUrl, string postUrl, params (string Key, string Value)[] fields)
    {
        var formPage = await client.GetStringAsync(formPageUrl);
        var token = Regex.Match(formPage, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

        var form = fields.ToDictionary(f => f.Key, f => f.Value);
        form["__RequestVerificationToken"] = token;

        return await client.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }
}
