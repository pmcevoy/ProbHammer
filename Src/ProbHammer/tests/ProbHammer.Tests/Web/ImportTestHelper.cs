using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ProbHammer.Tests.Web;

/// <summary>Shared helper for integration tests that need to drive the real `/Import` -> `/LivePlay`
/// flow end to end (session cookie included) rather than calling PageModel methods directly. Reads
/// the real captured exports from `data/` so these tests exercise the same real-world text the
/// manual verification (army-roster-enrichment's task 2.8) already validated against the live BSData
/// clone - the app's own bundled `BsData/` snapshot (checked into `src/ProbHammer.Web/BsData/`, not
/// excluded from source control, only from the publish content glob) is what resolves it here.</summary>
internal static partial class ImportTestHelper
{
    public static async Task<HttpResponseMessage> ImportAsync(HttpClient client, string exportText)
    {
        var getResponse = await client.GetAsync("/Import", TestContext.Current.CancellationToken);
        var html = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = ExtractAntiForgeryToken(html);

        var formData = new Dictionary<string, string>
        {
            ["ExportText"] = exportText,
            ["__RequestVerificationToken"] = token
        };

        return await client.PostAsync("/Import", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);
    }

    public static string ReadRealExport(string fileName, [CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "data", fileName));

    private static string ExtractAntiForgeryToken(string html)
    {
        var match = AntiForgeryTokenRegex().Match(html);
        if (!match.Success)
            throw new InvalidOperationException("Antiforgery token not found in /Import page response.");

        return match.Groups[1].Value;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiForgeryTokenRegex();
}
