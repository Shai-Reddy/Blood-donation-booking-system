using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using BloodDonation.Web.Data;
using BloodDonation.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Performance;

[TestClass]
[TestCategory("Performance")]
[DoNotParallelize]
public class EligibilityLoadTests
{
    private const int DonorCount = 20;
    private const int RoundCount = 10;
    // NFR2: an eligibility result within 2 seconds at up to 20 concurrent users (mid-project report).
    private const double P95LimitMilliseconds = 2000;
    private const string Password = "Load#Test1";
    private const string IntervalRejectionMessage = "At least 84 days must pass between donations.";

    private static readonly Regex AntiForgeryTokenPattern = new(
        "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task BookingEligibility_20ConcurrentDonors_P95WithinTwoSeconds()
    {
        var overallTimer = Stopwatch.StartNew();
        var session = CreatePerformanceSession(DonorCount * RoundCount + 1);
        var clients = new List<HttpClient>(DonorCount);
        var donors = new List<DonorClient>(DonorCount);
        var allResults = new List<TimedResponse>(DonorCount * RoundCount);

        try
        {
            using var factory = new WebApplicationFactory<Program>();
            using var warmupClient = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _ = factory.Server;
            var warmupTimer = Stopwatch.StartNew();
            using (var warmupResponse = await warmupClient.GetAsync("/Account/Register"))
            {
                Assert.AreEqual(HttpStatusCode.OK, warmupResponse.StatusCode, "The test host warm-up failed.");
            }
            warmupTimer.Stop();
            TestContext.WriteLine($"Host warm-up: {warmupTimer.Elapsed.TotalSeconds:F2} s");

            InMemoryStore.Sessions.Add(session);
            var registrationTimer = Stopwatch.StartNew();

            for (var index = 0; index < DonorCount; index++)
            {
                var donorTimer = Stopwatch.StartNew();
                var donor = await RegisterDonorAsync(factory, index + 1);
                donorTimer.Stop();

                donors.Add(donor);
                clients.Add(donor.Client);
                TestContext.WriteLine($"Registration {index + 1}/{DonorCount}: {donorTimer.Elapsed.TotalSeconds:F2} s");
            }

            registrationTimer.Stop();
            TestContext.WriteLine($"All registrations: {registrationTimer.Elapsed.TotalSeconds:F2} s");

            for (var round = 0; round < RoundCount; round++)
            {
                var roundTimer = Stopwatch.StartNew();
                var preparationTimer = Stopwatch.StartNew();
                var requests = new List<PreparedRequest>(DonorCount);
                var lastDonationDate = session.SessionDate
                    .AddDays(-60)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                foreach (var donor in donors)
                {
                    var form = await donor.Client.GetStringAsync("/Donor/Book");
                    var fields = new Dictionary<string, string>
                    {
                        ["SessionId"] = session.Id,
                        ["DonorName"] = "Eligibility Load Donor",
                        ["DonorEmail"] = donor.Email,
                        ["BloodType"] = "",
                        ["DateOfBirth"] = "1990-01-01",
                        ["WeightKg"] = "70",
                        ["LastDonationDate"] = lastDonationDate,
                        ["__RequestVerificationToken"] = ExtractAntiForgeryToken(form)
                    };
                    requests.Add(new PreparedRequest(donor, new FormUrlEncodedContent(fields)));
                }
                preparationTimer.Stop();

                var releaseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var readySignals = requests
                    .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
                    .ToArray();
                var roundTasks = requests.Select((request, index) => SendTimedBookingAsync(
                    request,
                    readySignals[index],
                    releaseGate.Task)).ToArray();

                await Task.WhenAll(readySignals.Select(signal => signal.Task));
                var postTimer = Stopwatch.StartNew();
                releaseGate.SetResult();
                var roundResults = await Task.WhenAll(roundTasks);
                postTimer.Stop();
                allResults.AddRange(roundResults);

                var rejectionTimer = Stopwatch.StartNew();
                for (var index = 0; index < roundResults.Length; index++)
                {
                    await AssertEligibilityRejectionAsync(roundResults[index], round * DonorCount + index + 1);
                }
                rejectionTimer.Stop();
                roundTimer.Stop();

                TestContext.WriteLine(
                    $"Round {round + 1}/{RoundCount}: preparation {preparationTimer.Elapsed.TotalSeconds:F2} s; " +
                    $"concurrent POSTs {postTimer.Elapsed.TotalSeconds:F2} s; " +
                    $"rejection checks {rejectionTimer.Elapsed.TotalSeconds:F2} s; " +
                    $"total {roundTimer.Elapsed.TotalSeconds:F2} s");
            }

            var orderedTimes = allResults
                .Select(result => result.Elapsed.TotalMilliseconds)
                .OrderBy(milliseconds => milliseconds)
                .ToArray();
            var average = orderedTimes.Average();
            var p95Rank = (int)Math.Ceiling(0.95 * orderedTimes.Length);
            var p95 = orderedTimes[p95Rank - 1];
            var maximum = orderedTimes[^1];

            TestContext.WriteLine($"Requests: {orderedTimes.Length} ({DonorCount} concurrent x {RoundCount} rounds)");
            TestContext.WriteLine($"Average response time: {average:F2} ms");
            TestContext.WriteLine($"Nearest-rank p95 response time: {p95:F2} ms (rank {p95Rank})");
            TestContext.WriteLine($"Maximum response time: {maximum:F2} ms");
            TestContext.WriteLine($"P95 limit (NFR2): {P95LimitMilliseconds:F0} ms");

            Assert.HasCount(DonorCount * RoundCount, allResults,
                "Not all timed booking requests completed.");

            var bookingsCreated = InMemoryStore.Bookings.Count(booking => booking.SessionId == session.Id);
            Assert.AreEqual(0, bookingsCreated,
                $"Expected no bookings for interval-ineligible requests; found {bookingsCreated}.");

            Assert.IsLessThanOrEqualTo(P95LimitMilliseconds,
                p95, $"NFR2 p95 was {p95:F2} ms; limit is {P95LimitMilliseconds:F0} ms.");
        }
        finally
        {
            InMemoryStore.Bookings.RemoveAll(booking => booking.SessionId == session.Id);
            InMemoryStore.Sessions.Remove(session);

            foreach (var client in clients)
            {
                client.Dispose();
            }

            overallTimer.Stop();
            TestContext.WriteLine($"Overall load-test execution: {overallTimer.Elapsed.TotalSeconds:F2} s");
        }
    }

    [TestMethod]
    [TestCategory("PerformanceSmoke")]
    public async Task BookingEligibility_SmokeTest_RejectsShortDonationInterval()
    {
        var overallTimer = Stopwatch.StartNew();
        var session = CreatePerformanceSession(capacity: 2);
        HttpClient? client = null;

        try
        {
            using var factory = new WebApplicationFactory<Program>();
            var donor = await RegisterDonorAsync(factory, 1);
            client = donor.Client;
            InMemoryStore.Sessions.Add(session);

            var preparationTimer = Stopwatch.StartNew();
            var form = await client.GetStringAsync("/Donor/Book");
            using var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SessionId"] = session.Id,
                ["DonorName"] = "Eligibility Smoke Donor",
                ["DonorEmail"] = donor.Email,
                ["BloodType"] = "",
                ["DateOfBirth"] = "1990-01-01",
                ["WeightKg"] = "70",
                ["LastDonationDate"] = session.SessionDate
                    .AddDays(-60)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["__RequestVerificationToken"] = ExtractAntiForgeryToken(form)
            });
            preparationTimer.Stop();
            TestContext.WriteLine($"Smoke-test preparation: {preparationTimer.Elapsed.TotalSeconds:F2} s");

            var postTimer = Stopwatch.StartNew();
            TimedResponse result;
            using (var response = await client.PostAsync("/Donor/Book", requestContent))
            {
                postTimer.Stop();
                result = new TimedResponse(donor, response.StatusCode, response.Headers.Location, postTimer.Elapsed);
            }

            await AssertEligibilityRejectionAsync(result, 1);

            var bookingsCreated = InMemoryStore.Bookings.Count(booking => booking.SessionId == session.Id);
            Assert.AreEqual(0, bookingsCreated,
                $"Expected no bookings for the interval-ineligible smoke request; found {bookingsCreated}.");
            TestContext.WriteLine($"Smoke-test booking POST: {postTimer.Elapsed.TotalMilliseconds:F2} ms");
        }
        finally
        {
            InMemoryStore.Bookings.RemoveAll(booking => booking.SessionId == session.Id);
            InMemoryStore.Sessions.Remove(session);
            client?.Dispose();
            overallTimer.Stop();
            TestContext.WriteLine($"Overall smoke-test execution: {overallTimer.Elapsed.TotalSeconds:F2} s");
        }
    }

    private static DonationSession CreatePerformanceSession(int capacity)
    {
        return new DonationSession
        {
            Id = $"PERF-{Guid.NewGuid():N}",
            SessionDate = DateTime.Today.AddDays(7),
            Time = "Performance test",
            Location = "Test only",
            Capacity = capacity,
            BookedCount = 0
        };
    }

    private static async Task<DonorClient> RegisterDonorAsync(WebApplicationFactory<Program> factory, int index)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var email = $"eligibility-load-{Guid.NewGuid():N}@test.local";

        try
        {
            using var registration = await PostWithTokenAsync(
                client,
                "/Account/Register",
                "/Account/Register",
                ("FullName", $"Load Donor {index}"),
                ("Email", email),
                ("Password", Password),
                ("ConfirmPassword", Password),
                ("DateOfBirth", "1990-01-01"),
                ("WeightKg", "70"),
                ("TravelledOverseas", "No"));

            Assert.AreEqual(HttpStatusCode.Redirect, registration.StatusCode,
                $"Registration failed for test donor {index}.");
            Assert.AreEqual("/Donor/Dashboard", RedirectPath(registration),
                $"Test donor {index} was not signed in after registration.");
            return new DonorClient(client, email);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static async Task<TimedResponse> SendTimedBookingAsync(
        PreparedRequest request,
        TaskCompletionSource readySignal,
        Task startSignal)
    {
        using var content = request.Content;
        readySignal.SetResult();
        await startSignal;

        var stopwatch = Stopwatch.StartNew();
        using var response = await request.Donor.Client.PostAsync("/Donor/Book", content);
        stopwatch.Stop();

        return new TimedResponse(request.Donor, response.StatusCode, response.Headers.Location, stopwatch.Elapsed);
    }

    private static async Task AssertEligibilityRejectionAsync(TimedResponse result, int requestNumber)
    {
        Assert.AreEqual(HttpStatusCode.Redirect, result.StatusCode,
            $"Request {requestNumber} did not redirect after booking eligibility rejection.");
        Assert.AreEqual("/Donor/Book", RedirectPath(result.Location),
            $"Request {requestNumber} did not redirect to the booking form.");

        // Consume and assert each donor's TempData before starting that donor's next round.
        var bookingForm = await result.Donor.Client.GetStringAsync("/Donor/Book");
        Assert.Contains(IntervalRejectionMessage, bookingForm,
            $"Request {requestNumber} did not show the 84-day interval rejection.");
    }

    private static async Task<HttpResponseMessage> PostWithTokenAsync(
        HttpClient client,
        string formPageUrl,
        string postUrl,
        params (string Key, string Value)[] fields)
    {
        var formPage = await client.GetStringAsync(formPageUrl);
        var form = fields.ToDictionary(field => field.Key, field => field.Value);
        form["__RequestVerificationToken"] = ExtractAntiForgeryToken(formPage);

        return await client.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = AntiForgeryTokenPattern.Match(html);
        Assert.IsTrue(match.Success, "The form did not contain an anti-forgery token.");
        return match.Groups[1].Value;
    }

    private static string RedirectPath(HttpResponseMessage response)
    {
        return RedirectPath(response.Headers.Location);
    }

    private static string RedirectPath(Uri? location)
    {
        Assert.IsNotNull(location, "The response did not include a redirect location.");
        return location.IsAbsoluteUri
            ? location.AbsolutePath
            : location.OriginalString.Split('?')[0];
    }

    private sealed record DonorClient(HttpClient Client, string Email);
    private sealed record PreparedRequest(DonorClient Donor, FormUrlEncodedContent Content);
    private sealed record TimedResponse(DonorClient Donor, HttpStatusCode StatusCode, Uri? Location, TimeSpan Elapsed);
}
