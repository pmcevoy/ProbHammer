using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>Loads/saves <c>classifications.json</c> - a hash-keyed JSON object (unlike
/// ability-corpus.json's array; this file is always looked up by hash, never browsed
/// name-first), pretty-printed for the same hand-editability/git-diffability reasons as the
/// extraction file.</summary>
public static class ClassificationFile
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        // Same "grep/vim searchable" reasoning as AbilityCorpusFile's own SerializerOptions - the
        // default encoder's \uXXXX escaping of '/+/</>/& etc defeats that outright.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IReadOnlyDictionary<string, ClassificationRecord> Load(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, ClassificationRecord>();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, ClassificationRecord>>(json, SerializerOptions)
               ?? new Dictionary<string, ClassificationRecord>();
    }

    public static void Save(string path, IReadOnlyDictionary<string, ClassificationRecord> records)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var ordered = records
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var json = JsonSerializer.Serialize(ordered, SerializerOptions);
        File.WriteAllText(path, json);
    }
}