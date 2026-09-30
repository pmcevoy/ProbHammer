using System.Runtime.CompilerServices;

namespace ProbHammer.Tools.AbilityPipeline.Extractor;

/// <summary>Stage 1 of the ability-classification pipeline. Walks the entire BSData corpus once
/// (via <see cref="CorpusWalker"/>, reusing <c>Domain.Catalogue.Bsdata</c>'s own already-hardened
/// closure-resolution/rule-walking machinery) and writes <c>ability-corpus.json</c>: one
/// deduplicated, hash-keyed record per distinct rule/ability/Enhancement/Detachment-rule text in
/// the corpus. Never invoked from any runtime path - an offline tool only.</summary>
public static class Program
{
    /// <summary>The bundled snapshot this repo actually ships and tests against
    /// (src/ProbHammer.Web/BsData/*.json) - not the external live clone
    /// <c>tools/RuleEffectClassificationReport</c> defaults to, so this tool runs out of the box on
    /// any checkout with no extra setup. Still overridable via the first command-line argument to
    /// point at the external live clone instead.</summary>
    private static string DefaultClonePath([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "src", "ProbHammer.Web", "BsData");

    private static string DefaultOutputPath([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "..", "data", "ability-corpus.json");

    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var clonePath = args.Length > 0 ? args[0] : DefaultClonePath();
        var outputPath = args.Length > 1 ? args[1] : DefaultOutputPath();

        if (!Directory.Exists(clonePath))
        {
            Console.WriteLine($"BSData corpus not found at '{clonePath}'.");
            return 1;
        }

        var previous = AbilityCorpusFile.Load(outputPath);
        var vocabulary = new CorpusVocabulary();
        var occurrences = CorpusWalker.Walk(clonePath, Console.Out, vocabulary).ToList();
        var aggregation = AbilityCorpusAggregator.Aggregate(occurrences, DateOnly.FromDateTime(DateTime.UtcNow));
        var merged = AbilityCorpusFile.Merge(previous, aggregation.FreshRecords);
        AbilityCorpusFile.Save(outputPath, merged);
        var vocabularyPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "vocabulary.json");
        vocabulary.Save(vocabularyPath);
        Console.WriteLine($"Wrote corpus vocabulary to '{vocabularyPath}'.");

        PrintSummary(clonePath, outputPath, previous, aggregation, merged);
        return 0;
    }

    private static void PrintSummary(string clonePath, string outputPath,
        IReadOnlyDictionary<string, AbilityCorpusRecord> previous, AggregationResult aggregation,
        IReadOnlyList<AbilityCorpusRecord> merged)
    {
        var stale = merged.Count - aggregation.FreshRecords.Count;
        var newCount = aggregation.FreshRecords.Keys.Count(h => !previous.ContainsKey(h));

        Console.WriteLine();
        Console.WriteLine($"Scanned '{clonePath}'.");
        Console.WriteLine(
            $"{aggregation.TotalRawOccurrences} raw Name+Text occurrences -> {aggregation.FreshRecords.Count} " +
            $"distinct texts this run ({(double)aggregation.TotalRawOccurrences / Math.Max(1, aggregation.FreshRecords.Count):F2}x repetition).");
        Console.WriteLine($"{newCount} newly seen, {aggregation.FreshRecords.Count - newCount} previously known, " +
                          $"{stale} present in the previous file but absent this run (kept, not dropped).");

        Console.WriteLine();
        Console.WriteLine("=== Distinct records per source kind (this run) ===");
        foreach (var group in aggregation.FreshRecords.Values
                     .GroupBy(r => r.SourceKind)
                     .OrderByDescending(g => g.Count()))
            Console.WriteLine($"    {group.Key}: {group.Count()}");

        // Ranked by distinct Names sharing one text, not raw occurrence count - raw count mostly
        // reflects how many datasheet entries happen to duplicate one Optional-grant/wargear-choice
        // string verbatim (a BSData authoring artifact, not a meaningful corpus-shape signal); a
        // text known under many distinct NAMES is the genuinely interesting case for "regaining a
        // mental model of the corpus" (e.g. one shared rule text granted under many different named
        // grants/wargear items).
        const int topN = 10;
        Console.WriteLine();
        Console.WriteLine($"=== Top {topN} texts by distinct ability-name count (one text, many names) ===");
        foreach (var record in aggregation.FreshRecords.Values
                     .OrderByDescending(r => r.Names.Count)
                     .Take(topN))
        {
            const int maxNamesShown = 6;
            var namesLabel = record.Names.Count <= maxNamesShown
                ? string.Join(", ", record.Names)
                : $"{string.Join(", ", record.Names.Take(maxNamesShown))}, +{record.Names.Count - maxNamesShown} more";
            Console.WriteLine($"    {record.Names.Count} names ({record.SourceKind})  [{namesLabel}]");
            Console.WriteLine($"        :: \"{Truncate(record.Text)}\"");
        }

        Console.WriteLine();
        Console.WriteLine($"Wrote {merged.Count} records to '{outputPath}'.");
    }

    private static string Truncate(string text)
    {
        const int maxLength = 120;
        var singleLine = text.ReplaceLineEndings(" ");
        return singleLine.Length > maxLength ? singleLine[..maxLength] + "..." : singleLine;
    }
}