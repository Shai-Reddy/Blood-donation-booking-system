using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BloodDonation.Tests.Integration;

internal static class IntegrationTestHelpers
{
    private static readonly Regex AntiForgeryTokenPattern = new(
        "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"",
        RegexOptions.Compiled);

    public static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public static string ExtractAntiForgeryToken(string html)
    {
        return AntiForgeryTokenPattern.Match(html).Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostWithTokenAsync(
        HttpClient client,
        string formPageUrl,
        string postUrl,
        params (string Key, string Value)[] fields)
    {
        var formPage = await client.GetStringAsync(formPageUrl);
        var token = ExtractAntiForgeryToken(formPage);
        var form = fields.ToDictionary(field => field.Key, field => field.Value);
        form["__RequestVerificationToken"] = token;

        return await client.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }
}
