using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>One human-verified rule/ability classification record in the checked-in baseline (see
/// baseline-rule-effect-classifications design.md's "Baseline file shape" decision) - the JSON
/// counterpart of a <see cref="RuleClassification"/>, keyed by the rule/ability's own normalized Text,
/// never Name (mirrors <c>RuleEffectClassificationReport</c>'s own existing grouping rationale -
/// <see cref="RuleEffectClassifier.Classify"/> never reads its <c>name</c> argument, so two different
/// Names sharing identical Text are the same classification result). <see cref="Note"/> is optional
/// free text recording that this entry's classification, while verified correct as far as it goes, is
/// known to omit real content its text states - mirrors an <c>AllowlistEntry&lt;T&gt;.Description</c>'s
/// own hand-authored convention elsewhere in this codebase; not present on most entries.</summary>
public sealed record RuleClassificationBaselineEntry(
    string Text,
    RuleTarget Target,
    IReadOnlyList<CharacteristicEffect> Effects,
    string? Note = null)
{
    [JsonIgnore] public RuleClassification Classification => new(Target, Effects);
}

/// <summary>The root JSON shape of the checked-in baseline file - a flat list, not keyed by Text in the
/// JSON itself (the in-memory <see cref="RuleClassificationBaseline"/> builds that index on load).</summary>
public sealed record RuleClassificationBaselineFile(IReadOnlyList<RuleClassificationBaselineEntry> Entries);

/// <summary>A checked-in, Text-keyed record of human-verified <see cref="RuleClassification"/>s - see
/// baseline-rule-effect-classifications proposal.md for why this exists (so a reviewer of the corpus
/// report never has to re-read an already-verified, unchanged result) and design.md for the file shape.
/// Mirrors <c>RuleGlossary</c>'s own "load once, query by key" shape.</summary>
public sealed class RuleClassificationBaseline
{
    /// <summary>Shared by every serialize/deserialize call site this baseline touches - camelCase
    /// property names (matching design.md's JSON example) and indented output, so a snapshot write
    /// produces a readable <c>git diff</c>. The relaxed encoder keeps ordinary punctuation real BSData
    /// rule text carries (an apostrophe, a "+") literal rather than escaped to <c>'</c>/<c>+</c>
    /// - this file is checked-in, trusted data, not attacker-controlled HTML, so the default encoder's
    /// conservative escaping only hurts the diff's own readability here.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Dictionary<string, RuleClassificationBaselineEntry> _entriesByText;

    private RuleClassificationBaseline(Dictionary<string, RuleClassificationBaselineEntry> entriesByText) =>
        _entriesByText = entriesByText;

    public IReadOnlyDictionary<string, RuleClassificationBaselineEntry> Entries => _entriesByText;

    /// <summary>A missing file loads as an empty baseline rather than throwing - lets a fresh checkout
    /// with no baseline yet (or a scratch path a test points at) run the report tool with every text
    /// simply unbaselined.</summary>
    public static RuleClassificationBaseline Load(string path)
    {
        if (!File.Exists(path))
            return new RuleClassificationBaseline(new Dictionary<string, RuleClassificationBaselineEntry>());

        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<RuleClassificationBaselineFile>(json, Options);
        var entriesByText = (file?.Entries ?? []).ToDictionary(e => e.Text);
        return new RuleClassificationBaseline(entriesByText);
    }

    public bool TryGet(string text, out RuleClassificationBaselineEntry entry) =>
        _entriesByText.TryGetValue(text, out entry!);

    /// <summary>Replaces (or adds) the tracked entry for this Text - used by <c>--write-baseline</c> to
    /// refresh a tracked entry's classification fields in place.</summary>
    public void Upsert(RuleClassificationBaselineEntry entry) => _entriesByText[entry.Text] = entry;

    /// <summary>Writes every currently-tracked entry back to <paramref name="path"/>, ordered by Text
    /// for a stable, readable diff.</summary>
    public void Save(string path)
    {
        var file = new RuleClassificationBaselineFile(
            [.. _entriesByText.Values.OrderBy(e => e.Text, StringComparer.Ordinal)]);
        var json = JsonSerializer.Serialize(file, Options);
        File.WriteAllText(path, json);
    }
}