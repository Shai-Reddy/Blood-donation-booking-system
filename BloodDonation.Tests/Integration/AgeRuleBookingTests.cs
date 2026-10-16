using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

// #25, FR3, TC07a, TC07b: the NZBS age rules applied through the donor booking form.
// The ages are written out from the NZBS criteria, not read from EligibilityRules,
// so a wrong constant in the domain makes these tests fail (the #2 lesson).
// Not parallelised: each eligible booking is added to the shared SampleDataStore, whose lists are not thread-safe.
[TestClass]
[DoNotParallelize]
public class AgeRuleBookingTests
{
    private const string DonorEmail = "donor@test.local";
    private const string DonorPassword = "Donor#Test1";
    private const string BookingUrl = "/Donor/Booking/Book";

    private const string Refused = "You're not eligible to book right now";
    private const string Booked = "Booking Confirmed";
    private const string NeedsAuthorisation = "Medical Officer";

    // NZBS: first-time donors until their 71st birthday; existing donors until their 75th,
    // then with Medical Officer authorisation until their 81st; lapsed donors past their 71st need authorisation.
    private const int FirstTimeLimit = 71;
    private const int ExistingLimit = 75;
    private const int AuthorisedLimit = 81;
    private const int LapsedLimit = 71;

    private static readonly DateTime Today = DateTime.Today;
    private static readonly DateTime ExistingDonation = Today.AddDays(-100);
    private static readonly DateTime LapsedDonation = Today.AddYears(-3);

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
    public async Task TC07a_FirstTimeDonorOn71stBirthday_IsRefused()
    {
        var body = await BookAsync(AgedOnToday(FirstTimeLimit), lastDonation: null);

        StringAssert.Contains(body, Refused);
    }

    [TestMethod]
    public async Task FirstTimeDonorDayBefore71stBirthday_CanBook()
    {
        var body = await BookAsync(DayBeforeBirthday(FirstTimeLimit), lastDonation: null);

        StringAssert.Contains(body, Booked);
    }

    [TestMethod]
    public async Task ExistingDonorDayBefore75thBirthday_CanBook()
    {
        var body = await BookAsync(DayBeforeBirthday(ExistingLimit), ExistingDonation);

        StringAssert.Contains(body, Booked);
    }

    [TestMethod]
    public async Task TC07b_ExistingDonorOn75thBirthday_IsToldToGetAuthorisation()
    {
        var body = await BookAsync(AgedOnToday(ExistingLimit), ExistingDonation);

        StringAssert.Contains(body, Refused);
        StringAssert.Contains(body, NeedsAuthorisation);
    }

    [TestMethod]
    public async Task ExistingDonorDayBefore81stBirthday_IsToldToGetAuthorisation()
    {
        var body = await BookAsync(DayBeforeBirthday(AuthorisedLimit), ExistingDonation);

        StringAssert.Contains(body, Refused);
        StringAssert.Contains(body, NeedsAuthorisation);
    }

    [TestMethod]
    public async Task ExistingDonorOn81stBirthday_IsRefused()
    {
        var body = await BookAsync(AgedOnToday(AuthorisedLimit), ExistingDonation);

        StringAssert.Contains(body, Refused);
    }

    [TestMethod]
    public async Task LapsedDonorDayBefore71stBirthday_CanBook()
    {
        var body = await BookAsync(DayBeforeBirthday(LapsedLimit), LapsedDonation);

        StringAssert.Contains(body, Booked);
    }

    [TestMethod]
    public async Task LapsedDonorOn71stBirthday_IsToldToGetAuthorisation()
    {
        var body = await BookAsync(AgedOnToday(LapsedLimit), LapsedDonation);

        StringAssert.Contains(body, Refused);
        StringAssert.Contains(body, NeedsAuthorisation);
    }

    // A donation exactly 2 years ago still counts as "in the last 2 years", so the donor is existing, not lapsed.
    [TestMethod]
    public async Task DonorWhoDonatedExactlyTwoYearsAgo_Aged72_CanBook()
    {
        var body = await BookAsync(AgedOnToday(72), Today.AddYears(-2));

        StringAssert.Contains(body, Booked);
    }

    private static DateTime AgedOnToday(int years) => Today.AddYears(-years);

    private static DateTime DayBeforeBirthday(int years) => Today.AddYears(-years).AddDays(1);

    private static async Task<string> BookAsync(DateTime dateOfBirth, DateTime? lastDonation)
    {
        var client = await SignedInDonorAsync();

        var form = new Dictionary<string, string>
        {
            ["DateOfBirth"] = dateOfBirth.ToString("yyyy-MM-dd"),
            ["WeightKg"] = "70",
            ["LastDonationDate"] = lastDonation?.ToString("yyyy-MM-dd") ?? "",
            ["HasStandDownEvent"] = "false",
            ["__RequestVerificationToken"] = await TokenAsync(client, BookingUrl),
        };

        var response = await client.PostAsync(BookingUrl, new FormUrlEncodedContent(form));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The booking form should return a result page.");
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<HttpClient> SignedInDonorAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = DonorEmail,
            ["Password"] = DonorPassword,
            ["__RequestVerificationToken"] = await TokenAsync(client, "/Account/Login"),
        });

        var response = await client.PostAsync("/Account/Login", form);
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, "Donor login failed.");
        return client;
    }

    private static async Task<string> TokenAsync(HttpClient client, string formPageUrl)
    {
        var page = await client.GetStringAsync(formPageUrl);
        return Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
    }
}
