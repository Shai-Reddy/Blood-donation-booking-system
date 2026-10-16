using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

[TestClass]
[DoNotParallelize]
public class ClientValidationTests
{
    // Test credentials used to sign in as donor and staff users.
    private const string DonorPassword = "Donor#Test1";
    private const string StaffEmail = "staff@test.local";
    private const string StaffPassword = "Staff#Test1";

    // Shared test application factory.
    private static WebApplicationFactory<Program> _factory = null!;

    // Create the test application once before all tests run.
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        _factory = new WebApplicationFactory<Program>();
        _ = _factory.Server;
    }

    // Dispose the test application after all tests have finished.
    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory.Dispose();
    }

    // TC18: The donor booking page should load the client-side
    // validation scripts and validation metadata.
    [TestMethod]
    public async Task TC18_DonorBook_LoadsClientValidation()
    {
        // Register and sign in a donor so the booking page can be accessed.
        var client = await RegisteredDonorAsync();

        // Load the donor booking page.
        var response = await client.GetAsync("/Donor/Book");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // Read the rendered HTML so the validation scripts and
        // validation metadata can be checked.
        var html = await response.Content.ReadAsStringAsync();

        AssertClientValidationLoaded(html);
    }

    // TC18: The staff record outcome page should load
    // the client-side validation scripts and metadata.
    [TestMethod]
    public async Task TC18_StaffRecordOutcome_LoadsClientValidation()
    {
        // A donor books first, so the staff bookings page has a row
        // with a Record Outcome form on it.
        var donor = await RegisteredDonorAsync();

        var booking = await PostWithTokenAsync(
            donor,
            "/Donor/Book",
            "/Donor/Book",
            ("SessionId", "S1"),
            ("DonorName", "Validation Donor"),
            ("DonorEmail", "validation.donor@test.local"),
            ("BloodType", ""),
            ("DateOfBirth", "1990-01-01"),
            ("WeightKg", "70"),
            ("LastDonationDate", ""),
            ("HasStandDownEvent", "false"));

        Assert.AreEqual(
            HttpStatusCode.Redirect,
            booking.StatusCode,
            "Booking failed.");

        // Sign in as a staff user.
        var client = await StaffClientAsync();

        // Load the bookings page staff use to record outcomes (#35).
        var response = await client.GetAsync("/Staff/Bookings");

        Assert.AreEqual(
            HttpStatusCode.OK,
            response.StatusCode);

        // Read the rendered HTML so the validation scripts and
        // validation metadata can be checked.
        var html = await response.Content.ReadAsStringAsync();

        StringAssert.Contains(
            html,
            "/Staff/RecordOutcome",
            "The Record Outcome form was not on the staff bookings page.");

        AssertClientValidationLoaded(html);
    }

    // TC18: The staff session management page should load
    // the client-side validation scripts and metadata.
    [TestMethod]
    public async Task TC18_StaffSessions_LoadsClientValidation()
    {
        // Sign in as a staff user.
        var client = await StaffClientAsync();

        // Load the staff sessions page.
        var response = await client.GetAsync("/Staff/Sessions");

        Assert.AreEqual(
            HttpStatusCode.OK,
            response.StatusCode);

        // Read the rendered HTML so the validation scripts and
        // validation metadata can be checked.
        var html = await response.Content.ReadAsStringAsync();

        AssertClientValidationLoaded(html);
    }

    // TC18: The registration page should load the client-side
    // validation scripts and validation metadata for signed-out users.
    [TestMethod]
    public async Task TC18_Register_LoadsClientValidation()
    {
        // Registration is available without signing in.
        using var client = NewClient();

        // Load the registration page.
        var response = await client.GetAsync("/Account/Register");

        Assert.AreEqual(
            HttpStatusCode.OK,
            response.StatusCode);

        // Read the rendered HTML so the validation scripts and
        // validation metadata can be checked.
        var html = await response.Content.ReadAsStringAsync();

        AssertClientValidationLoaded(html);
    }

    #region Helpers

    // Checks that the page includes both:
    // 1. jQuery unobtrusive validation JavaScript.
    // 2. Validation metadata generated for required fields.
    private static void AssertClientValidationLoaded(string html)
    {
        StringAssert.Contains(
            html,
            "jquery.validate.unobtrusive",
            "The jQuery unobtrusive validation script was not loaded.");

        StringAssert.Contains(
            html,
            "data-val-required",
            "No data-val-required validation metadata was rendered.");
    }

    // Creates a new HTTP client for the test application.
    private static HttpClient NewClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                // Prevent automatic redirects so individual test
                // responses can be checked directly.
                AllowAutoRedirect = false
            });
    }

    // Signs in as the seeded staff account and returns the authenticated client.
    private static async Task<HttpClient> StaffClientAsync()
    {
        var client = NewClient();

        // Get the login page first so the anti-forgery token can be
        // included if one is present.
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

    // Registers a new donor account and returns the authenticated client.
    private static async Task<HttpClient> RegisteredDonorAsync()
    {
        var client = NewClient();

        // Use a unique email so each test registration does not
        // conflict with an existing donor account.
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

    // Gets the form page, extracts its anti-forgery token if one exists,
    // and then submits the supplied form fields.
    private static async Task<HttpResponseMessage> PostWithTokenAsync(
        HttpClient client,
        string formPageUrl,
        string postUrl,
        params (string Key, string Value)[] fields)
    {
        // Load the form first so we can retrieve its anti-forgery token.
        var formPage = await client.GetStringAsync(formPageUrl);

        var token = ExtractToken(formPage);

        // Convert the supplied fields into form data.
        var form = fields.ToDictionary(
            f => f.Key,
            f => f.Value);

        // Include the anti-forgery token when the page provides one.
        if (!string.IsNullOrEmpty(token))
        {
            form["__RequestVerificationToken"] = token;
        }

        return await client.PostAsync(
            postUrl,
            new FormUrlEncodedContent(form));
    }

    // Extracts the anti-forgery token from the rendered HTML.
    private static string ExtractToken(string htmlContent)
    {
        // Handle the normal ASP.NET Core generated order:
        // name -> type -> value.
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

        // Return the token if found, otherwise return an empty string.
        return match.Success ? match.Groups[1].Value : "";
    }

    #endregion
}