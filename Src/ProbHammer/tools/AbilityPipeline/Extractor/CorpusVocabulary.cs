using System.Text.Encodings.Web;
using System.Text.Json;

namespace ProbHammer.Tools.AbilityPipeline.Extractor;

/// <summary>Every BSData name the classifier's collect-time resolution checks model output against,
/// each spelling counted by occurrence so the classifier can pick the most common one as canonical.
/// Written by the extractor because the classifier deliberately doesn't reference ProbHammer.Core.</summary>
public sealed class CorpusVocabulary
{
    public SortedDictionary<string, int> WeaponKeywords { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, int> WeaponNames { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, int> UnitKeywords { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, int> AbilityNames { get; } = new(StringComparer.Ordinal);

    public static void Count(SortedDictionary<string, int> counts, string spelling)
    {
        var trimmed = spelling.Trim();
        if (trimmed.Length > 0)
            counts[trimmed] = counts.GetValueOrDefault(trimmed) + 1;
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }));
    }
}