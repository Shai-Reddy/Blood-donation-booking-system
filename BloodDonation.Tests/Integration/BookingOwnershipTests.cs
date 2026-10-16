using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

// D3 (#3), NFR3, FR5: a donor can only see, cancel or reschedule their own booking.
// Each test registers its own donors through the real register form, so tests never share a donor.
// Not parallelised: these tests add and remove bookings in the static InMemoryStore, whose lists are not thread-safe.
[TestClass]
[DoNotParallelize]
public class BookingOwnershipTests
{
    private const string Password = "Owner#Test1";

    // S3 is a seeded session with spare capacity (InMemoryStore).
    private const string SessionId = "S3";

    private static WebApplicationFactory<Program> _factory = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>();
        // Start the host once here; parallel tests would otherwise race to start it.
        _ = _factory.Server;
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory.Dispose();
    }

    [TestMethod]
    public async Task DonorCancelsAnotherDonorsBooking_IsDenied()
    {
        var (owner, bookingId) = await DonorWithBookingAsync();
        var other = await RegisteredDonorAsync();

        var response = await IntegrationTestHelpers.PostWithTokenAsync(other, "/Donor/Dashboard", "/Donor/Cancel", ("BookingId", bookingId));

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/Account/AccessDenied", RedirectPath(response));
        Assert.AreEqual(bookingId, await DashboardBookingIdAsync(owner), "The owner's booking should still exist.");
    }

    [TestMethod]
    public async Task DonorReschedulesAnotherDonorsBooking_IsDenied()
    {
        var (owner, bookingId) = await DonorWithBookingAsync();
        var other = await RegisteredDonorAsync();

        var response = await IntegrationTestHelpers.PostWithTokenAsync(other, "/Donor/Dashboard", "/Donor/Reschedule", ("BookingId", bookingId));

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/Account/AccessDenied", RedirectPath(response));
        Assert.AreEqual(bookingId, await DashboardBookingIdAsync(owner), "The owner's booking should still exist.");
    }

    [TestMethod]
    public async Task DonorCancelsOwnBooking_IsAllowed()
    {
        var (owner, bookingId) = await DonorWithBookingAsync();

        var response = await IntegrationTestHelpers.PostWithTokenAsync(owner, "/Donor/Dashboard", "/Donor/Cancel", ("BookingId", bookingId));

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/Donor/Dashboard", RedirectPath(response));
        Assert.IsNull(await DashboardBookingIdAsync(owner), "The cancelled booking should be gone.");
    }

    [TestMethod]
    public async Task DonorDashboard_DoesNotShowAnotherDonorsBooking()
    {
        var (_, bookingId) = await DonorWithBookingAsync();
        var other = await RegisteredDonorAsync();

        var dashboard = await other.GetStringAsync("/Donor/Dashboard");

        Assert.DoesNotContain(bookingId, dashboard, "Another donor's booking ID appeared on this dashboard.");
    }

    private static async Task<(HttpClient Client, string BookingId)> DonorWithBookingAsync()
    {
        var client = await RegisteredDonorAsync();

        var response = await IntegrationTestHelpers.PostWithTokenAsync(client, "/Donor/Book", "/Donor/Book",
            ("SessionId", SessionId),
            ("DonorName", "Test Donor"),
            ("DonorEmail", "test.donor@test.local"),
            ("BloodType", ""),
            ("DateOfBirth", "1999-01-02"),
            ("WeightKg", "70"));
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, "Booking failed.");

        var bookingId = await DashboardBookingIdAsync(client);
        Assert.IsNotNull(bookingId, "The new booking should appear on the owner's dashboard.");
        return (client, bookingId);
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
            ("WeightKg", "70"),
            ("TravelledOverseas", "No"));
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, $"Registering {email} failed.");
        return client;
    }

    // The cookie handler redirects with an absolute URL, RedirectToAction with a relative one.
    private static string RedirectPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location!;
        return location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString.Split('?')[0];
    }

    private static async Task<string?> DashboardBookingIdAsync(HttpClient client)
    {
        var dashboard = await client.GetStringAsync("/Donor/Dashboard");
        var match = Regex.Match(dashboard, "name=\"BookingId\" value=\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

}
