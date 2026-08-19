namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// The bidirectional allowlist check both corpus scans share: every produced result must match
/// an <see cref="AllowlistEntry{T}"/>, and every allowlist entry must have matched at least one
/// produced result during the run - so an unrecognized result and a stale (no-longer-hit)
/// allowlist entry are both treated as regressions (see design.md and this capability's
/// "Bidirectional Allowlist Enforcement" requirement). Both directions are checked from one
/// collected result set in a single pass, since only the complete set can tell a stale entry
/// apart from one about to match a result the scan simply hasn't produced yet.
/// </summary>
public static class AllowlistCheck
{
    /// <summary>
    /// Fails the test (via <see cref="Assert.Fail"/>) if any result is unmatched by the
    /// allowlist, or any allowlist entry matched nothing - listing both kinds of mismatch
    /// separately in the failure message. Passes silently otherwise.
    /// </summary>
    public static void AssertClean<T>(
        IReadOnlyList<T> results,
        IReadOnlyList<AllowlistEntry<T>> allowlist,
        Func<T, string> describeResult)
    {
        var unmatchedResults = new List<T>();
        var hitEntries = new HashSet<AllowlistEntry<T>>();

        foreach (var result in results)
        {
            var matchingEntry = allowlist.FirstOrDefault(entry => entry.Matches(result));
            if (matchingEntry is null)
                unmatchedResults.Add(result);
            else
                hitEntries.Add(matchingEntry);
        }

        var unmatchedEntries = allowlist.Where(entry => !hitEntries.Contains(entry)).ToList();
        if (unmatchedResults.Count == 0 && unmatchedEntries.Count == 0)
            return;

        var lines = new List<string>();

        if (unmatchedResults.Count > 0)
        {
            lines.Add($"{unmatchedResults.Count} result(s) not covered by the allowlist:");
            lines.AddRange(unmatchedResults.Select(result => $"  - {describeResult(result)}"));
        }

        if (unmatchedEntries.Count > 0)
        {
            lines.Add($"{unmatchedEntries.Count} allowlist entry/entries not hit by this run " +
                      "(stale - remove, or the underlying issue may have regressed):");
            lines.AddRange(unmatchedEntries.Select(entry => $"  - {entry.Description}"));
        }

        Assert.Fail(string.Join("\n", lines));
    }
}
