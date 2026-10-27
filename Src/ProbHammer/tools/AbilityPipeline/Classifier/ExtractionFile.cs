using System.Text.Json;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

public static class ExtractionFile
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyDictionary<string, ExtractionRecord> Load(string path)
    {
        var json = File.ReadAllText(path);
        var records = JsonSerializer.Deserialize<List<ExtractionRecord>>(json, SerializerOptions)
                       ?? throw new InvalidDataException($"'{path}' deserialized to no records.");
        return records.ToDictionary(r => r.Hash);
    }

    /// <summary>The records the latest extraction saw. The Extractor keeps a hash it no longer finds,
    /// with its old <see cref="ExtractionRecord.LastSeenAt"/>, so being in the corpus isn't being in BSData.</summary>
    public static IReadOnlyDictionary<string, ExtractionRecord> Live(
        this IReadOnlyDictionary<string, ExtractionRecord> corpus)
    {
        if (corpus.Count == 0)
            return corpus;

        var latest = corpus.Values.Max(r => r.LastSeenAt);
        return corpus.Where(kvp => kvp.Value.LastSeenAt == latest).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
