namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Finds the closest candidate name to an unresolved piece of export text, for a "did you mean...?"
/// hint on a resolution-failure diagnostic. This is a fail-loud enhancement, not a resolution
/// strategy - it never changes whether a name resolves, only what the thrown exception says. Chosen
/// over maintaining a curated mismatch map (e.g. "Absolvor bolt pistol" in a captured export vs.
/// BSData's own "Absolver bolt pistol" - a real confirmed BSData spelling difference): the BSData
/// corpus is too large and too fluid for a hand-maintained table to stay complete, whereas an edit-
/// distance check against whatever names actually exist keeps working as the corpus changes.
/// </summary>
public static class BsdataNameSuggestion
{
    public static string? FindClosest(string target, IEnumerable<string> candidates, int maxDistance = 3)
    {
        string? best = null;
        var bestDistance = int.MaxValue;

        foreach (var candidate in candidates)
        {
            var distance = LevenshteinDistance(target, candidate);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return bestDistance <= maxDistance ? best : null;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();

        var previousRow = Enumerable.Range(0, b.Length + 1).ToArray();
        var currentRow = new int[b.Length + 1];

        for (var i = 1; i <= a.Length; i++)
        {
            currentRow[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                currentRow[j] = Math.Min(
                    Math.Min(currentRow[j - 1] + 1, previousRow[j] + 1),
                    previousRow[j - 1] + cost);
            }

            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[b.Length];
    }
}
