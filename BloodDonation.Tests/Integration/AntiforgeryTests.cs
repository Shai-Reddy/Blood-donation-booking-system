using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

// NFR3: POST actions must reject requests that do not contain
// a valid anti-forgery token.
[TestClass]
[DoNotParallelize]
public class AntiforgeryTests
{
    private const string DonorPassword = "Donor#Test1";
    private const string StaffEmail = "staff@test.local";
    private const string StaffPassword = "Staff#Test1";

    // S3 is a seeded session with spare capacity.
    private const string SessionId = "S3";

    private static WebApplicationFactory<Program> _factory = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>();

        // Start the host once here because the tests use the
        // shared in-memory booking store.
        _ = _factory.Server;
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Antiforgery_CancelWithoutToken_IsRejected()
    {
        // Create a donor and booking using the normal form flow.
        var (client, bookingId) = await DonorWithBookingAsync();

        // Submit the cancellation directly without first getting
        // or sending an anti-forgery token.
        var response = await client.PostAsync(
            "/Donor/Cancel",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["BookingId"] = bookingId
                }));

        // A POST without a valid token must be rejected.
        Assert.AreEqual(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        // The rejected request must not remove the donor's booking.
        Assert.IsTrue(
            BloodDonation.Web.Data.InMemoryStore.Bookings
                .Any(b => b.Id == bookingId),
            "The booking should still exist after a POST without an anti-forgery token.");
    }

    [TestMethod]
    public async Task Antiforgery_RecordOutcomeWithoutToken_IsRejected()
    {
        // Create a booking so staff have an existing booking
        // to try to update.
        var (_, bookingId) = await DonorWithBookingAsync();

        // Sign in as the seeded staff user.
        var staffClient = await StaffClientAsync();

        // Submit the outcome directly without an anti-forgery token.
        var response = await staffClient.PostAsync(
            "/Staff/RecordOutcome",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["BookingId"] = bookingId,
                    ["Outcome"] = "Deferred",
                    ["DeferralReason"] = "Test reason"
                }));

        // A POST without a valid token must be rejected.
        Assert.AreEqual(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [TestMethod]
    public async Task Antiforgery_CancelWithToken_Works()
    {
        // Create a donor and booking using the normal form flow.
        var (client, bookingId) = await DonorWithBookingAsync();

        // Get the donor dashboard first so its anti-forgery token
        // can be included in the cancellation request.
        var response = await PostWithTokenAsync(
            client,
            "/Donor/Dashboard",
            "/Donor/Cancel",
            ("BookingId", bookingId));

        // A POST containing a valid token should continue to work.
        Assert.AreEqual(
            HttpStatusCode.Redirect,
            response.StatusCode);

        // The successful cancellation should remove the booking.
        Assert.IsFalse(
            BloodDonation.Web.Data.InMemoryStore.Bookings
                .Any(b => b.Id == bookingId),
            "The booking should be removed when cancellation contains a valid anti-forgery token.");
    }

    #region Helpers

    private static HttpClient NewClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    private static async Task<(HttpClient Client, string BookingId)> DonorWithBookingAsync()
    {
        var client = await RegisteredDonorAsync();

        // Book the seeded S3 session using the real booking form
        // and its anti-forgery token.
        var response = await PostWithTokenAsync(
            client,
            "/Donor/Book",
            "/Donor/Book",
            ("SessionId", SessionId),
            ("DonorName", "Test Donor"),
            ("DonorEmail", "test.donor@test.local"),
            ("BloodType", ""),
            ("DateOfBirth", "1999-01-02"),
            ("WeightKg", "70"));

        Assert.AreEqual(
            HttpStatusCode.Redirect,
            response.StatusCode,
            "Booking failed.");

        // Read the donor dashboard to retrieve the new booking id.
        var dashboard = await client.GetStringAsync("/Donor/Dashboard");

        var match = Regex.Match(
            dashboard,
            @"name=""BookingId""\s+value=""([^""]+)""",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            match = Regex.Match(
                dashboard,
                @"value=""([^""]+)""\s+name=""BookingId""",
                RegexOptions.IgnoreCase);
        }

        Assert.IsTrue(
            match.Success,
            $"Could not extract booking ID from dashboard HTML:\n{dashboard}");

        return (client, match.Groups[1].Value);
    }

    private static async Task<HttpClient> RegisteredDonorAsync()
    {
        var client = NewClient();
        var email = $"donor-{Guid.NewGuid():N}@test.local";

        // Register through the real registration form so the
        // returned client is signed in as a donor.
        var response = await PostWithTokenAsync(
            client,
            "/Account/Register",
            "/Account/Register",
            ("FullName", "Test Donor"),
            ("Email", email),
            ("Password", DonorPassword),
            ("ConfirmPassword", DonorPassword),
            ("DateOfBirth", "1990-01-01"),
            ("WeightKg", "70"),
            ("TravelledOverseas", "No"));

        Assert.AreEqual(
            HttpStatusCode.Redirect,
            response.StatusCode,
            $"Registering {email} failed.");

        return client;
    }

    private static async Task<HttpClient> StaffClientAsync()
    {
        var client = NewClient();

        // Sign in through the real staff login form.
        var response = await PostWithTokenAsync(
            client,
            "/Account/StaffLogin",
            "/Account/StaffLogin",
            ("Email", StaffEmail),
            ("Password", StaffPassword));

        Assert.AreEqual(
            HttpStatusCode.Redirect,
            response.StatusCode,
            $"Login as {StaffEmail} failed.");

        return client;
    }

    private static async Task<HttpResponseMessage> PostWithTokenAsync(
        HttpClient client,
        string formPageUrl,
        string postUrl,
        params (string Key, string Value)[] fields)
    {
        // Load the form page first so its anti-forgery token
        // can be extracted.
        var formPage = await client.GetStringAsync(formPageUrl);
        var token = ExtractToken(formPage);

        Assert.IsFalse(
            string.IsNullOrEmpty(token),
            $"No anti-forgery token was found on {formPageUrl}.");

        var form = fields.ToDictionary(
            f => f.Key,
            f => f.Value);

        form["__RequestVerificationToken"] = token;

        return await client.PostAsync(
            postUrl,
            new FormUrlEncodedContent(form));
    }

    private static string ExtractToken(string htmlContent)
    {
        // Handle the normal ASP.NET Core generated attribute order.
        var match = Regex.Match(
            htmlContent,
            @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""",
            RegexOptions.IgnoreCase);

        // Also handle the reverse attribute order if encountered.
        if (!match.Success)
        {
            match = Regex.Match(
                htmlContent,
                @"value=""([^""]+)""\s+name=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase);
        }

        return match.Success ? match.Groups[1].Value : "";
    }

    #endregion
}