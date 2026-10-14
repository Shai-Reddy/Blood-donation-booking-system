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
    private const double P95LimitMilliseconds = 2000;
    private const string Password = "Load#Test1";

    private static readonly Regex AntiForgeryTokenPattern = new(
        "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task BookingEligibility_20ConcurrentDonors_P95WithinTwoSeconds()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var warmupClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _ = factory.Server;
        using (var warmupResponse = await warmupClient.GetAsync("/Account/Register"))
        {
            Assert.AreEqual(HttpStatusCode.OK, warmupResponse.StatusCode, "The test host warm-up failed.");
        }

        var session = new DonationSession
        {
            Id = $"PERF-{Guid.NewGuid():N}",
            SessionDate = DateTime.Today.AddDays(7),
            Time = "Performance test",
            Location = "Test only",
            Capacity = DonorCount * RoundCount + 1,
            BookedCount = 0
        };
        InMemoryStore.Sessions.Add(session);

        var clients = new List<HttpClient>(DonorCount);
        var donors = new List<DonorClient>(DonorCount);
        var allResults = new List<TimedResponse>(DonorCount * RoundCount);
        var lastDonationDate = DateTime.Today.AddDays(-60).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            for (var index = 0; index < DonorCount; index++)
            {
                var client = factory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });
                clients.Add(client);

                var email = $"eligibility-load-{Guid.NewGuid():N}@test.local";
                var registration = await PostWithTokenAsync(
                    client,
                    "/Account/Register",
                    "/Account/Register",
                    ("FullName", $"Load Donor {index + 1}"),
                    ("Email", email),
                    ("Password", Password),
                    ("ConfirmPassword", Password),
                    ("DateOfBirth", "1990-01-01"),
                    ("WeightKg", "70"),
                    ("TravelledOverseas", "No"));

                using (registration)
                {
                    Assert.AreEqual(HttpStatusCode.Redirect, registration.StatusCode,
                        $"Registration failed for test donor {index + 1}.");
                    Assert.AreEqual("/Donor/Dashboard", RedirectPath(registration),
                        $"Test donor {index + 1} was not signed in after registration.");
                }

                donors.Add(new DonorClient(client, email));
            }

            for (var round = 0; round < RoundCount; round++)
            {
                var requests = new List<PreparedRequest>(DonorCount);
                foreach (var donor in donors)
                {
                    var form = await donor.Client.GetStringAsync("/Donor/Book");
                    var fields = new Dictionary<string, string>
                    {
                        ["SessionId"] = session.Id,
                        ["DonorName"] = "Eligibility Load Donor",
                        ["DonorEmail"] = donor.Email,
                        ["BloodType"] = "",
                        ["LastDonationDate"] = lastDonationDate,
                        ["__RequestVerificationToken"] = ExtractAntiForgeryToken(form)
                    };
                    requests.Add(new PreparedRequest(donor, new FormUrlEncodedContent(fields)));
                }

                var releaseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var readySignals = requests
                    .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
                    .ToArray();
                var roundTasks = requests.Select((request, index) => SendTimedBookingAsync(
                    request,
                    readySignals[index],
                    releaseGate.Task)).ToArray();

                await Task.WhenAll(readySignals.Select(signal => signal.Task));
                releaseGate.SetResult();
                allResults.AddRange(await Task.WhenAll(roundTasks));
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

            Assert.HasCount(DonorCount * RoundCount, allResults,
                "Not all timed booking requests completed.");

            for (var index = 0; index < allResults.Count; index++)
            {
                AssertIsNotAuthenticationOrAntiForgeryFailure(allResults[index], index + 1);
            }

            Assert.IsLessThanOrEqualTo(p95, P95LimitMilliseconds,
                $"NFR2 p95 was {p95:F2} ms; limit is {P95LimitMilliseconds:F0} ms.");

            var bookingsCreated = InMemoryStore.Bookings.Count(booking => booking.SessionId == session.Id);
            Assert.AreEqual(0, bookingsCreated,
                $"Expected no bookings for interval-ineligible requests; found {bookingsCreated}.");

            // Pending: /Donor/Book has no verified interval-specific rejection contract yet.
            // Do not infer rejection from a status code or invent a body/message assertion.
            // The observed responses are included below to make the missing contract visible.
            var observedResponses = string.Join(
                Environment.NewLine,
                allResults.Select((result, index) =>
                    $"Request {index + 1}: HTTP {(int)result.StatusCode}; body: {SummarizeBody(result.Body)}"));
            Assert.Inconclusive(
                "Pending eligibility fix: /Donor/Book must expose a verifiable, interval-specific " +
                "rejection before this test can establish that each request failed the 84-day rule. " +
                "Observed responses (not classified as interval rejections):" + Environment.NewLine +
                observedResponses);
        }
        finally
        {
            InMemoryStore.Bookings.RemoveAll(booking => booking.SessionId == session.Id);
            InMemoryStore.Sessions.Remove(session);

            foreach (var client in clients)
            {
                client.Dispose();
            }
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
        var body = await response.Content.ReadAsStringAsync();
        stopwatch.Stop();

        return new TimedResponse(response.StatusCode, response.Headers.Location, stopwatch.Elapsed, body);
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

    private static void AssertIsNotAuthenticationOrAntiForgeryFailure(TimedResponse result, int requestNumber)
    {
        Assert.AreNotEqual(HttpStatusCode.Unauthorized, result.StatusCode,
            $"Request {requestNumber} was unauthenticated.");
        Assert.AreNotEqual(HttpStatusCode.Forbidden, result.StatusCode,
            $"Request {requestNumber} was forbidden.");
        Assert.AreNotEqual(HttpStatusCode.BadRequest, result.StatusCode,
            $"Request {requestNumber} returned HTTP 400; treat this as an invalid request or anti-forgery failure, not a successful performance sample.");

        if (result.StatusCode == HttpStatusCode.Redirect && result.Location is not null)
        {
            var redirectPath = result.Location.IsAbsoluteUri
                ? result.Location.AbsolutePath
                : result.Location.OriginalString.Split('?')[0];
            Assert.AreNotEqual("/Account/Login", redirectPath,
                $"Request {requestNumber} was redirected to login.");
            Assert.AreNotEqual("/Account/AccessDenied", redirectPath,
                $"Request {requestNumber} was redirected to access denied.");
        }
    }

    private static string SummarizeBody(string body)
    {
        const int maxCharacters = 240;
        var normalized = Regex.Replace(body, @"\s+", " ").Trim();
        return normalized.Length <= maxCharacters
            ? normalized
            : normalized[..maxCharacters] + "...";
    }

    private static string RedirectPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location!;
        return location.IsAbsoluteUri
            ? location.AbsolutePath
            : location.OriginalString.Split('?')[0];
    }

    private sealed record DonorClient(HttpClient Client, string Email);
    private sealed record PreparedRequest(DonorClient Donor, FormUrlEncodedContent Content);
    private sealed record TimedResponse(HttpStatusCode StatusCode, Uri? Location, TimeSpan Elapsed, string Body);
}
