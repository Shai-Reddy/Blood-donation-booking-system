using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

[TestClass]
[DoNotParallelize]
public class StaffOutcomeTests
{
    private const string DonorPassword = "Donor#Test1";
    private const string StaffEmail = "staff@test.local";
    private const string StaffPassword = "Staff#Test1";
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
    public async Task TC11_DeferredWithoutReason_IsRejected()
    {
        // create a registered donor and booking for test
        var (_, bookingId) = await DonorWithBookingAsync();
        // sign in as stafff so the protected staff endpoint can be accessed
        var staffClient = await StaffClientAsync();
        //fetch bookings page to get the anti-forgery token and form data
        var bookingsUrl = "/Staff/Bookings";
        var formPageHtml = await staffClient.GetStringAsync(bookingsUrl);

        // submit deferral without a reason
        var emptyReasonResponse = await PostWithTokenFromPageAsync(
            staffClient,
            formPageHtml,
            "/Staff/RecordOutcome",
            ("BookingId", bookingId),
            ("Outcome", "Deferred"),
            ("DeferralReason", "")
        );

        Assert.AreEqual(HttpStatusCode.OK, emptyReasonResponse.StatusCode);

        var emptyResponseBody = await emptyReasonResponse.Content.ReadAsStringAsync();

        //form should be redisplayed with validation error message
        StringAssert.Contains(
            emptyResponseBody,
            "A reason is required when the donor is deferred.");

        // Booking must not have been changed if reason is missing
        Assert.IsFalse(
            Regex.IsMatch(
                emptyResponseBody,
                @"<td>\s*Deferred\s*</td>",
                RegexOptions.IgnoreCase),
            "Booking should not be changed to Deferred when no reason is supplied.");

        // submit deferral with whitespace reason only.
        var whitespaceResponse = await PostWithTokenFromPageAsync(
            staffClient,
            emptyResponseBody,
            "/Staff/RecordOutcome",
            ("BookingId", bookingId),
            ("Outcome", "Deferred"),
            ("DeferralReason", "   ")
        );

        Assert.AreEqual(HttpStatusCode.OK, whitespaceResponse.StatusCode);

        var whitespaceResponseBody = await whitespaceResponse.Content.ReadAsStringAsync();

        StringAssert.Contains(
            whitespaceResponseBody,
            "A reason is required when the donor is deferred.");

        // Booking must still not have been changed.
        Assert.IsFalse(
            Regex.IsMatch(
                whitespaceResponseBody,
                @"<td>\s*Deferred\s*</td>",
                RegexOptions.IgnoreCase),
            "Booking should not be changed to Deferred when only whitespace is supplied.");
    }

    [TestMethod]
    public async Task DeferredWithReason_IsSaved()
    {
        // 1. Setup donor & booking
        var (_, bookingId) = await DonorWithBookingAsync();
        var staffClient = await StaffClientAsync();

        // 2. Fetch the staff bookings page containing the outcome form
        var bookingsUrl = "/Staff/Bookings";
        var formPageHtml = await staffClient.GetStringAsync(bookingsUrl);

        // 3. POST valid deferral reason
        var response = await PostWithTokenFromPageAsync(
            staffClient,
            formPageHtml,
            "/Staff/RecordOutcome",
            ("BookingId", bookingId),
            ("Outcome", "Deferred"),
            ("DeferralReason", "Low haemoglobin")
        );

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);

        // 4. Verify the old StaffController saved the status and reason.
        var booking = BloodDonation.Web.Data.InMemoryStore.Bookings
            .FirstOrDefault(b => b.Id == bookingId);

        Assert.IsNotNull(booking);
        Assert.AreEqual("Deferred", booking.Status);
        Assert.AreEqual("Low haemoglobin", booking.DeferralReason);
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

    private static async Task<HttpClient> StaffClientAsync()
    {
        var client = NewClient();

        var response = await PostLoginAsync(
            client,
            "/Account/StaffLogin",
            StaffEmail,
            StaffPassword);

        Assert.AreEqual(
            HttpStatusCode.Redirect,
            response.StatusCode,
            $"Login as {StaffEmail} failed.");

        return client;
    }

    private static async Task<(HttpClient Client, string BookingId)> DonorWithBookingAsync()
    {
        var client = await RegisteredDonorAsync();

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

        var location = response.Headers.Location?.ToString() ?? "";

        var locationMatch = Regex.Match(
            location,
            @"[?&]bookingId=([^&]+)",
            RegexOptions.IgnoreCase);

        if (locationMatch.Success)
        {
            return (client, locationMatch.Groups[1].Value);
        }

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

        if (!match.Success)
        {
            match = Regex.Match(
                dashboard,
                @"/Donor/Cancel[?/]([^""'\s]+)",
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

    private static async Task<HttpResponseMessage> PostLoginAsync(
        HttpClient client,
        string loginUrl,
        string email,
        string password)
    {
        return await PostWithTokenAsync(
            client,
            loginUrl,
            loginUrl,
            ("Email", email),
            ("Password", password));
    }

    private static string ExtractToken(string htmlContent)
    {
        var match = Regex.Match(
            htmlContent,
            @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            match = Regex.Match(
                htmlContent,
                @"value=""([^""]+)""\s+name=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase);
        }

        return match.Success ? match.Groups[1].Value : "";
    }

    private static async Task<HttpResponseMessage> PostWithTokenAsync(
        HttpClient client,
        string formPageUrl,
        string postUrl,
        params (string Key, string Value)[] fields)
    {
        var formPage = await client.GetStringAsync(formPageUrl);

        return await PostWithTokenFromPageAsync(
            client,
            formPage,
            postUrl,
            fields);
    }

    private static Task<HttpResponseMessage> PostWithTokenFromPageAsync(
        HttpClient client,
        string formPageHtml,
        string postUrl,
        params (string Key, string Value)[] fields)
    {
        var token = ExtractToken(formPageHtml);

        var form = fields.ToDictionary(
            f => f.Key,
            f => f.Value);

        if (!string.IsNullOrEmpty(token))
        {
            form["__RequestVerificationToken"] = token;
        }

        return client.PostAsync(
            postUrl,
            new FormUrlEncodedContent(form));
    }

    #endregion
}