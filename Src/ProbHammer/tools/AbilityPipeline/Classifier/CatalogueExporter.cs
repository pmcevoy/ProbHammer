using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ProbHammer.Tools.AbilityPipeline.Classifier.Models;

namespace ProbHammer.Tools.AbilityPipeline.Classifier;

/// <summary>Writes the web app's ability-classification catalogue in the JSON shape
/// <c>ProbHammer.Core</c>'s <c>AbilityClassificationCatalogue</c> loads - by convention, not by
/// reference, so a Core test loading the real exported file is what catches drift.</summary>
public static class CatalogueExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Strings the model used for an unqualified Feel No Pain instead of the prompt's null.
    private static readonly HashSet<string> UnqualifiedFeelNoPainSentinels =
        new(["", "null", "none", "all", "__none__"], StringComparer.OrdinalIgnoreCase);

    public static string Export(IReadOnlyDictionary<string, ClassificationRecord> classifications,
        IReadOnlyDictionary<string, ExtractionRecord> corpus)
    {
        var records = new JsonArray();
        foreach (var (hash, record) in classifications.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            corpus.TryGetValue(hash, out var extraction);
            var classification = JsonSerializer.SerializeToNode(
                Canonicalize(record.Classification, record.Resolution), Options)!.AsObject();
            ReshapeInvulnerableSaves(classification);

            records.Add(new JsonObject
            {
                ["hash"] = hash,
                ["text"] = extraction?.Text ?? record.Text,
                ["names"] = new JsonArray([.. (extraction?.Names ?? []).Select(n => JsonValue.Create(n))]),
                ["classification"] = classification
            });
        }

        return new JsonObject { ["records"] = records }.ToJsonString(Options);
    }

    private static ClassificationResult Canonicalize(ClassificationResult classification, NameResolution? resolution)
    {
        var canonical = (resolution?.Resolved ?? [])
            .GroupBy(r => (r.Kind, r.Verbatim))
            .ToDictionary(g => g.Key, g => g.First().Canonical);

        string Name(string kind, string verbatim) => canonical.GetValueOrDefault((kind, verbatim), verbatim);

        WeaponSelector Selector(WeaponSelector selector) => selector is NamedWeaponSelector named
            ? new NamedWeaponSelector(Name("WeaponName", named.Name))
            : selector;

        ClassificationEffect Effect(ClassificationEffect effect) => effect switch
        {
            WeaponCharacteristicEffect weapon => weapon with { Selector = Selector(weapon.Selector) },
            WeaponKeywordGrantEffect grant => new WeaponKeywordGrantEffect(
                Selector(grant.Selector),
                Name("WeaponKeyword", grant.Keyword),
                grant.ReplacesKeyword is { } replaced ? Name("WeaponKeyword", replaced) : null),
            NamedAbilityGrantEffect abilityGrant =>
                new NamedAbilityGrantEffect(Name("AbilityName", abilityGrant.AbilityName)),
            FeelNoPainEffect { Qualifier: { } qualifier } feelNoPain
                when UnqualifiedFeelNoPainSentinels.Contains(qualifier.Trim()) => feelNoPain with { Qualifier = null },
            _ => effect
        };

        return classification with
        {
            Target = classification.Target is KeywordTarget keywordTarget
                ? new KeywordTarget([.. keywordTarget.Keywords.Select(k => Name("UnitKeyword", k))])
                : classification.Target,
            Effects = [.. classification.Effects.Select(e => e with { Effect = Effect(e.Effect) })]
        };
    }

    // Core wraps the melee/ranged pair in an InvulnerableSave value; the classifier's effect is flat.
    private static void ReshapeInvulnerableSaves(JsonObject classification)
    {
        foreach (var classified in classification["effects"]!.AsArray())
        {
            var effect = classified!["effect"]!.AsObject();
            if (effect["kind"]!.GetValue<string>() != "InvulnerableSave")
                continue;

            classified["effect"] = new JsonObject
            {
                ["kind"] = "InvulnerableSave",
                ["value"] = new JsonObject
                {
                    ["meleeInSv"] = effect["meleeInSv"]!.GetValue<int>(),
                    ["rangedInSv"] = effect["rangedInSv"]!.GetValue<int>()
                }
            };
        }
    }
}