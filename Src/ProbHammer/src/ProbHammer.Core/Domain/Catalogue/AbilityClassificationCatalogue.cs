using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>One exported pipeline record. <see cref="Text"/> and <see cref="Names"/> are for
/// findability and the hash check; lookup is by <see cref="Hash"/> alone.</summary>
public sealed record AbilityClassificationRecord(
    string Hash,
    string Text,
    IReadOnlyList<string> Names,
    AbilityClassification Classification);

public sealed record AbilityClassificationFile(IReadOnlyList<AbilityClassificationRecord> Records);

/// <summary>The runtime's checked-in ability classifications, keyed by
/// <see cref="AbilityTextKey.Hash"/> of an ability's text. Loaded once.</summary>
public sealed class AbilityClassificationCatalogue
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly Dictionary<string, AbilityClassificationRecord> _recordsByHash;

    private AbilityClassificationCatalogue(Dictionary<string, AbilityClassificationRecord> recordsByHash) =>
        _recordsByHash = recordsByHash;

    public static readonly AbilityClassificationCatalogue Empty = new([]);

    public IReadOnlyCollection<AbilityClassificationRecord> Records => _recordsByHash.Values;

    public static AbilityClassificationCatalogue FromRecords(IEnumerable<AbilityClassificationRecord> records) =>
        new(records.ToDictionary(r => r.Hash));

    /// <summary>Builds a catalogue keyed by each text's own hash - the convenient form for tests.</summary>
    public static AbilityClassificationCatalogue FromTexts(
        IEnumerable<(string Text, AbilityClassification Classification)> entries) =>
        FromRecords(entries.Select(e =>
            new AbilityClassificationRecord(AbilityTextKey.Hash(e.Text), e.Text, [], e.Classification)));

    public static AbilityClassificationCatalogue Load(string path)
    {
        if (!File.Exists(path))
            return Empty;

        var file = JsonSerializer.Deserialize<AbilityClassificationFile>(File.ReadAllText(path), Options);
        return FromRecords(file?.Records ?? []);
    }

    public bool TryGet(string abilityText, out AbilityClassification classification)
    {
        if (_recordsByHash.TryGetValue(AbilityTextKey.Hash(abilityText), out var record))
        {
            classification = record.Classification;
            return true;
        }

        classification = null!;
        return false;
    }
}
