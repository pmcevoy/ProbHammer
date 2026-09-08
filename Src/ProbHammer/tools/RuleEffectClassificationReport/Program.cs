using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tools.RuleEffectClassificationReport;

/// <summary>Corpus-wide manual-inspection tool for <see cref="RuleEffectClassifier"/> - see
/// classify-rule-effects-from-text proposal.md's "Corpus-Wide Classification Reporting" and
/// tasks.md's task 4. Walks the live BSData clone the same way this project's own
/// <c>[Fact(Explicit = true)]</c> CorpusScan tests do (see
/// tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/), collecting every rule/ability
/// Name+Text pair it can reach - local + shared rules, always-enumerated <see cref="Datasheet.Abilities"/>,
/// on-demand OptionalGrant/Enhancement abilities (<see cref="Datasheet.OptionalAbilityNames"/>), and
/// Detachment rule text - classifies each with <see cref="RuleEffectClassifier.Classify"/>, and
/// prints three sections: results with 1+ Effects (the ones worth eyeballing to confirm an extracted
/// Effect actually matches the text), results with a Target broader than Self but no Effect, and
/// results that classified to the all-default result - so real-corpus classification coverage can be
/// inspected directly, not just through unit-test pass/fail output.
///
/// Grouped by <see cref="RuleEffectClassifier.Normalize"/>d Text alone, not by (Name, Text) and not
/// by raw Text - <see cref="RuleEffectClassifier.Classify"/> never reads its <c>name</c> parameter,
/// so identical Text always classifies identically regardless of Name; keying on Name too would
/// artificially split one real classification result across several report rows purely because
/// different wargear/abilities share the exact same rules sentence (real example: "The bearer has a
/// 4+ invulnerable save." is granted, verbatim, by Astartes Shield, Blizzard shield, Brute Shield,
/// and several others). Grouping on the raw (un-normalized) Text has the identical problem one level
/// down - a typographic-apostrophe or NBSP-vs-plain-space variant of one real sentence (confirmed:
/// Ancient's Banner/Vexilla's own text exists both ways in the corpus) is something Classify already
/// treats as the same input, so the report groups it the same way rather than showing it twice. Every
/// distinct Name seen for a given (normalized) Text is still reported, as data on the row rather than
/// as part of the grouping key.</summary>
public static class Program
{
    /// <summary>Same literal path already documented in CLAUDE.md/.claude/domain-model-11e.md and
    /// hardcoded by <c>LiveClone.ClonePath</c> in the test project - this machine only. Overridable
    /// via the first command-line argument so the tool isn't hardwired to one machine.</summary>
    public const string DefaultClonePath = @"C:\Users\Pete\wh40k-11e";

    private const string ExcludedFileName = "Warhammer 40,000.json";

    public static int Main(string[] args)
    {
        // The process's default console encoding can't represent every character real BSData text
        // carries (e.g. a U+00A0 non-breaking space authoring quirk - see
        // InvulnerableSaveCaveatClassifier's own doc comment for the same quirk elsewhere), and
        // silently substitutes a garbage byte instead of erroring - forcing UTF-8 makes every
        // character in a Truncate()d text preview round-trip correctly regardless of terminal or
        // redirect target.
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var clonePath = args.Length > 0 ? args[0] : DefaultClonePath;

        if (!Directory.Exists(clonePath))
        {
            Console.WriteLine(
                $"BSData clone not found at '{clonePath}' - pass a path as the first argument, or clone the live BSData 11th-edition repo to that location.");
            return 1;
        }

        var source = new LocalDiskBsdataCatalogueSource(clonePath);
        var fileNames = source.ListFileNames()
            .Where(name => !string.Equals(name, ExcludedFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var occurrencesByText = new Dictionary<string, TextOccurrence>();
        var scannedSharedRules = false;

        foreach (var fileName in fileNames)
        {
            BsdataClosure closure;
            try
            {
                closure = BsdataClosureResolver.Resolve(source, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Skipping '{fileName}': closure resolution failed ({ex.Message})");
                continue;
            }

            var glossary = RuleGlossary.Build(closure);
            var idIndex = BsdataNameResolver.BuildIdIndex(closure);
            var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
            var profileIndex = BsdataNameResolver.BuildProfileIdIndex(closure);

            foreach (var rule in closure.Files[0].Catalogue.Rules)
                Collect(rule.Name, rule.Description, $"{fileName} :: rule");

            if (!scannedSharedRules && closure.GameSystem is not null)
            {
                scannedSharedRules = true;
                foreach (var rule in closure.GameSystem.SharedRules)
                    Collect(rule.Name, rule.Description, "(game system) :: shared rule");
            }

            foreach (var detachmentEntry in BsdataNameResolver.ResolveDetachmentEntries(closure))
            {
                foreach (var (name, text) in DetachmentRuleTextExtractor.Extract(detachmentEntry, glossary))
                    Collect(name, text, $"{fileName} :: detachment '{detachmentEntry.Name}'");
            }

            foreach (var entry in closure.Files[0].Catalogue.SharedSelectionEntries)
            {
                Datasheet datasheet;
                try
                {
                    datasheet = BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, profileIndex);
                }
                catch
                {
                    // Characteristic-resolution failures are covered by CharacteristicResolutionScanTests -
                    // an entry that fails to build a Datasheet at all has no Ability.Text to collect here.
                    continue;
                }

                foreach (var ability in datasheet.Abilities)
                    Collect(ability.Name, ability.Text, $"{fileName} :: '{entry.Name}' ability");

                foreach (var optionalName in datasheet.OptionalAbilityNames)
                {
                    if (datasheet.TryResolveAbility(optionalName, out var ability))
                        Collect(ability.Name, ability.Text, $"{fileName} :: '{entry.Name}' optional ability");
                }
            }
        }

        var results = occurrencesByText
            .Select(kvp =>
            {
                var names = kvp.Value.Names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
                return (Text: kvp.Key, Names: names, kvp.Value.Locations,
                    Classification: RuleEffectClassifier.Classify(names[0], kvp.Key));
            })
            .ToList();

        // Three-way split, not the spec's original two-way non-default/default-only: results with
        // 1+ Effects are their own section (the one worth eyeballing to confirm an extracted Effect
        // actually matches what the text says), separate from results whose Target is broader than
        // Self but which extracted no Effect at all (Templar Vows/Faith-Fuelled Resolve-shaped -
        // still a "non-default" classification per the spec, just not an Effect-review candidate).
        // An earlier version of this split (Effects-only vs. everything-else) silently dropped the
        // Target-only bucket from BOTH sections - caught live, not by any test.
        var effectResults = results
            .Where(r => r.Classification.Effects.Count > 0)
            .OrderBy(r => r.Names[0], StringComparer.OrdinalIgnoreCase)
            .ToList();

        var targetOnlyResults = results
            .Where(r => r.Classification.Target is not SelfRuleTarget && r.Classification.Effects.Count == 0)
            .OrderBy(r => r.Names[0], StringComparer.OrdinalIgnoreCase)
            .ToList();

        var defaultOnly = results
            .Where(r => r.Classification.Target is SelfRuleTarget && r.Classification.Effects.Count == 0)
            .OrderBy(r => r.Names[0], StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine();
        Console.WriteLine($"Scanned {fileNames.Count} catalogue files, {results.Count} distinct rule/ability texts.");
        Console.WriteLine();
        Console.WriteLine($"=== Effect results: {effectResults.Count} ===");
        foreach (var r in effectResults)
        {
            var namesLabel = DescribeNames(r.Names);
            var extra = r.Locations.Count > 3 ? $", +{r.Locations.Count - 3} more" : "";
            Console.WriteLine($"- {namesLabel} -> {Describe(r.Classification)}");
            Console.WriteLine($"    text: \"{Truncate(r.Text)}\"");
            Console.WriteLine($"    seen on: {string.Join("; ", r.Locations.Take(3))}{extra}");
        }

        Console.WriteLine();
        Console.WriteLine($"=== Target-only results (no Effect): {targetOnlyResults.Count} ===");
        foreach (var r in targetOnlyResults)
        {
            var namesLabel = DescribeNames(r.Names);
            var extra = r.Locations.Count > 3 ? $", +{r.Locations.Count - 3} more" : "";
            Console.WriteLine($"- {namesLabel} -> {Describe(r.Classification)}");
            Console.WriteLine($"    text: \"{Truncate(r.Text)}\"");
            Console.WriteLine($"    seen on: {string.Join("; ", r.Locations.Take(3))}{extra}");
        }

        const int sampleSize = 25;
        Console.WriteLine();
        Console.WriteLine(
            $"=== Default-only results: {defaultOnly.Count} (showing first {Math.Min(sampleSize, defaultOnly.Count)} for spot-checking) ===");
        foreach (var r in defaultOnly.Take(sampleSize))
            Console.WriteLine($"- {DescribeNames(r.Names)} :: \"{Truncate(r.Text)}\" (seen on: {r.Locations[0]})");

        return 0;

        void Collect(string name, string text, string location)
        {
            // Group by RuleEffectClassifier's own normalized text, not the raw text - otherwise two
            // texts Classify() would treat as identical (a typographic apostrophe or NBSP variant of
            // the same sentence) show up as separate rows purely because of a normalization Classify
            // already applies internally. Confirmed real: Ancient's Banner/Vexilla's own text exists
            // in the corpus with a plain space and, separately, with an NBSP in the same spot.
            var normalized = RuleEffectClassifier.Normalize(text);
            if (!occurrencesByText.TryGetValue(normalized, out var occurrence))
                occurrencesByText[normalized] = occurrence = new TextOccurrence();
            occurrence.Names.Add(name);
            occurrence.Locations.Add($"{location} :: '{name}'");
        }
    }

    private static string DescribeNames(List<string> names) =>
        names.Count == 1 ? $"'{names[0]}'" : $"[{string.Join(", ", names)}]";

    private static string Truncate(string text)
    {
        const int maxLength = 300;
        var singleLine = text.ReplaceLineEndings(" ");
        return singleLine.Length > maxLength ? singleLine[..maxLength] + "..." : singleLine;
    }

    private static string Describe(RuleClassification classification)
    {
        var target = classification.Target switch
        {
            SelfRuleTarget => "Self",
            AttachedUnitRuleTarget => "AttachedUnit",
            KeywordRuleTarget keyword => $"Keyword(\"{keyword.Keyword}\")",
            UnconditionalRuleTarget => "Unconditional",
            _ => classification.Target.ToString()!
        };

        var effects = classification.Effects.Count == 0
            ? "no effects"
            : string.Join(", ", classification.Effects.Select(e => $"{e.Verb} {e.Characteristic} {e.Amount}"));

        return $"Target={target}; Effects=[{effects}]";
    }

    private sealed class TextOccurrence
    {
        public SortedSet<string> Names { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Locations { get; } = [];
    }
}