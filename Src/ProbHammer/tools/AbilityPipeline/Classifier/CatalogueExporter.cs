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

    /// <summary>Exports only live hashes (see <see cref="ExtractionFile.Live"/>). <see cref="CatalogueExport.Json"/>
    /// is null when any exported classification string holds a control character other than a line feed.</summary>
    public static CatalogueExport Export(IReadOnlyDictionary<string, ClassificationRecord> classifications,
        IReadOnlyDictionary<string, ExtractionRecord> corpus)
    {
        var live = corpus.Live();
        var records = new JsonArray();
        var leftOut = 0;
        var controlCharacters = new List<string>();
        foreach (var (hash, record) in classifications.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
        {
            if (!live.TryGetValue(hash, out var extraction))
            {
                leftOut++;
                continue;
            }

            var classification = JsonSerializer.SerializeToNode(
                Canonicalize(record.Classification, record.Resolution), Options)!.AsObject();
            ReshapeInvulnerableSaves(classification);
            ReshapeWeaponSelectors(classification);
            FindControlCharacters(classification, "classification", hash, controlCharacters);

            records.Add(new JsonObject
            {
                ["hash"] = hash,
                ["text"] = extraction.Text,
                ["names"] = new JsonArray([.. extraction.Names.Select(n => JsonValue.Create(n))]),
                ["classification"] = classification
            });
        }

        var json = controlCharacters.Count == 0 ? new JsonObject { ["records"] = records }.ToJsonString(Options) : null;
        return new CatalogueExport(json, leftOut, controlCharacters);
    }

    private static void FindControlCharacters(JsonNode? node, string path, string hash, List<string> found)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, child) in obj)
                    FindControlCharacters(child, $"{path}.{name}", hash, found);
                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                    FindControlCharacters(array[i], $"{path}[{i}]", hash, found);
                break;
            case JsonValue value when value.TryGetValue<string>(out var text) &&
                                      text.Any(c => char.IsControl(c) && c != '\n'):
                found.Add($"{hash} {path}");
                break;
        }
    }

    private static ClassificationResult Canonicalize(ClassificationResult classification, NameResolution? resolution)
    {
        var canonical = (resolution?.Resolved ?? [])
            .GroupBy(r => (r.Kind, r.Verbatim))
            .ToDictionary(g => g.Key, g => g.First().Canonical);

        string Name(string kind, string verbatim) => canonical.GetValueOrDefault((kind, verbatim), verbatim);

        string? WeaponName(string? weaponName) => weaponName is null ? null : Name("WeaponName", weaponName);

        ClassificationEffect Effect(ClassificationEffect effect) => effect switch
        {
            WeaponCharacteristicEffect weapon => weapon with { WeaponName = WeaponName(weapon.WeaponName) },
            RollModifierEffect roll => roll with { WeaponName = WeaponName(roll.WeaponName) },
            WeaponKeywordGrantEffect grant => grant with
            {
                Keyword = Name("WeaponKeyword", grant.Keyword),
                ReplacesKeyword = grant.ReplacesKeyword is { } replaced ? Name("WeaponKeyword", replaced) : null,
                WeaponName = WeaponName(grant.WeaponName)
            },
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

    // Core's weapon effects carry a WeaponSelector; of roll modifiers, only Hit, Wound and Damage rolls do.
    private static void ReshapeWeaponSelectors(JsonObject classification)
    {
        foreach (var classified in classification["effects"]!.AsArray())
        {
            var effect = classified!["effect"]!.AsObject();
            var kind = effect["kind"]!.GetValue<string>();
            if (kind is not ("WeaponCharacteristic" or "WeaponKeywordGrant" or "RollModifier"))
                continue;

            var weaponClass = effect["weaponClass"]?.GetValue<string>();
            var weaponName = effect["weaponName"]?.GetValue<string>();
            effect.Remove("weaponClass");
            effect.Remove("weaponName");
            if (kind == "RollModifier" && effect["roll"]!.GetValue<string>() is not ("Hit" or "Wound" or "Damage"))
                continue;

            effect["selector"] = weaponName is not null
                ? new JsonObject { ["kind"] = "NamedWeapon", ["name"] = weaponName }
                : weaponClass is not null
                    ? new JsonObject { ["kind"] = "WeaponClass", ["type"] = weaponClass }
                    : new JsonObject { ["kind"] = "AllWeapons" };
        }
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

/// <summary><see cref="Json"/> is null when <see cref="ControlCharacters"/> lists any "hash field" problem.</summary>
public sealed record CatalogueExport(string? Json, int LeftOut, IReadOnlyList<string> ControlCharacters);