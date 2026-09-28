using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tools.AbilityPipeline.Extractor;

public sealed record AggregationResult(
    IReadOnlyDictionary<string, AbilityCorpusRecord> FreshRecords,
    int TotalRawOccurrences);

/// <summary>Groups a corpus walk's raw occurrences by <see cref="RuleEffectClassifier.Normalize"/>d
/// text hash into one <see cref="AbilityCorpusRecord"/> per distinct text - the dedup step behind
/// the extraction tool's "one record per distinct text" guarantee.</summary>
public static class AbilityCorpusAggregator
{
    public static AggregationResult Aggregate(IEnumerable<RawOccurrence> occurrences, DateOnly asOf)
    {
        var names = new Dictionary<string, SortedSet<string>>();
        var texts = new Dictionary<string, string>();
        var bestKind = new Dictionary<string, string>();
        var total = 0;

        foreach (var occurrence in occurrences)
        {
            total++;
            var normalized = RuleEffectClassifier.Normalize(occurrence.Text);
            var hash = ContentHash.Of(normalized);

            if (texts.TryAdd(hash, normalized))
            {
                names[hash] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                bestKind[hash] = occurrence.SourceKind;
            }

            names[hash].Add(occurrence.Name);

            // The most specific kind observed for this hash wins - see SourceKinds.Specificity.
            if (SourceKinds.Specificity[occurrence.SourceKind] < SourceKinds.Specificity[bestKind[hash]])
                bestKind[hash] = occurrence.SourceKind;
        }

        var records = texts.Keys.ToDictionary(hash => hash, hash => new AbilityCorpusRecord
        {
            Hash = hash,
            Text = texts[hash],
            Names = names[hash].ToList(),
            SourceKind = bestKind[hash],
            FirstSeenAt = asOf,
            LastSeenAt = asOf
        });

        return new AggregationResult(records, total);
    }
}