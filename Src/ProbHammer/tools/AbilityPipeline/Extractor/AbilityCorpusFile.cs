using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Extractor;

/// <summary>Loads/saves <c>ability-corpus.json</c> and merges a fresh aggregation run against the
/// previous file's own records, per this tool's presence-tracking contract: a hash missing from
/// this run's fresh output is kept, unmodified, rather than dropped - see
/// <see cref="AbilityCorpusRecord.LastSeenAt"/>.</summary>
public static class AbilityCorpusFile
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        // System.Text.Json's default encoder escapes '/+/</>/& etc as \uXXXX (HTML-embedding
        // safety this file never needs) - defeats the "grep/vim searchable" requirement outright
        // (searching this file for a literal "Feel No Pain 4+" or "Oath of Moment's" would find
        // nothing). This file never leaves the local filesystem, so the relaxed encoder is safe.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IReadOnlyDictionary<string, AbilityCorpusRecord> Load(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, AbilityCorpusRecord>();

        var json = File.ReadAllText(path);
        var records = JsonSerializer.Deserialize<List<AbilityCorpusRecord>>(json, SerializerOptions) ?? [];
        return records.ToDictionary(r => r.Hash);
    }

    /// <summary>Merges this run's freshly aggregated records against the previously-saved file:
    /// a hash present in both keeps its original <see cref="AbilityCorpusRecord.FirstSeenAt"/> and
    /// advances <see cref="AbilityCorpusRecord.LastSeenAt"/> to this run; a hash present only in
    /// the fresh run is new; a hash present only in the previous file is carried forward
    /// untouched, making its absence from this run visible via a stale
    /// <see cref="AbilityCorpusRecord.LastSeenAt"/> rather than silently disappearing.</summary>
    public static IReadOnlyList<AbilityCorpusRecord> Merge(
        IReadOnlyDictionary<string, AbilityCorpusRecord> previous,
        IReadOnlyDictionary<string, AbilityCorpusRecord> fresh)
    {
        var merged = new Dictionary<string, AbilityCorpusRecord>(previous);

        foreach (var (hash, record) in fresh)
        {
            merged[hash] = previous.TryGetValue(hash, out var existing)
                ? record with { FirstSeenAt = existing.FirstSeenAt }
                : record;
        }

        return merged.Values
            .OrderBy(r => r.Names.Count > 0 ? r.Names[0] : r.Hash, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void Save(string path, IReadOnlyList<AbilityCorpusRecord> records)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(records, SerializerOptions);
        File.WriteAllText(path, json);
    }
}