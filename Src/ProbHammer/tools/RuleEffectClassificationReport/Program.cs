using System.Runtime.CompilerServices;
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
/// as part of the grouping key.
///
/// Since baseline-rule-effect-classifications, a text with a matching entry in the checked-in
/// <see cref="RuleClassificationBaseline"/> (src/ProbHammer.Web/Data/RuleEffectClassifications.json)
/// is diverted from its normal Effect/Target-only/default-only section into a separate "Changed since
/// verified" listing driven by <see cref="RuleClassificationDiff"/> - an unchanged, already-verified
/// result collapses to a summary count instead of reprinting, while any drift is always surfaced. See
/// that change's proposal.md/design.md for the full rationale.
///
/// Since widen-rule-effect-classification-coverage, a further "Caveated baseline entries needing
/// review" section lists every baselined text whose current <see cref="RuleClassification.IsCaveated"/>
/// is true and whose baseline entry carries no <see cref="RuleClassificationBaselineEntry.Note"/> yet -
/// independent of the Unchanged/Drift/NewInformation split above, since a caveated-and-unchanged entry
/// would otherwise collapse into the summary count and never prompt a return visit. A human reviewing
/// this list either extends the classifier to capture the extra content, or accepts the entry as-is by
/// hand-adding a `note` recording that decision - the only field this tool never computes on a human's
/// behalf. Adding the note is what removes the entry from this list on the next run.</summary>
public static class Program
{
    /// <summary>Same literal path already documented in CLAUDE.md/.claude/domain-model-11e.md and
    /// hardcoded by <c>LiveClone.ClonePath</c> in the test project - this machine only. Overridable
    /// via the first command-line argument so the tool isn't hardwired to one machine.</summary>
    public const string DefaultClonePath = @"C:\Users\Pete\wh40k-11e";

    private const string ExcludedFileName = "Warhammer 40,000.json";

    /// <summary>Unlike <see cref="DefaultClonePath"/> (genuinely outside the repo and
    /// machine-specific), the baseline file lives inside this repo - resolved <c>[CallerFilePath]</c>-
    /// relative from this tool's own source file, the same portable-across-machines/checkouts
    /// convention <c>ArmyListParserTests.ReadDataFile</c> already uses for checked-in fixtures. Still
    /// overridable via a second command-line argument, mirroring the clone-path argument's own
    /// precedent - see baseline-rule-effect-classifications design.md's "Report tool's own path
    /// resolution" decision.</summary>
    private static string DefaultBaselinePath([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "src", "ProbHammer.Web", "Data",
            "RuleEffectClassifications.json");

    public static int Main(string[] args)
    {
        // The process's default console encoding can't represent every character real BSData text
        // carries (e.g. a U+00A0 non-breaking space authoring quirk - see
        // InvulnerableSaveCaveatClassifier's own doc comment for the same quirk elsewhere), and
        // silently substitutes a garbage byte instead of erroring - forcing UTF-8 makes every
        // character in a Truncate()d text preview round-trip correctly regardless of terminal or
        // redirect target.
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var writeBaseline = args.Contains("--write-baseline");
        var positionalArgs = args.Where(a => a != "--write-baseline").ToArray();
        var clonePath = positionalArgs.Length > 0 ? positionalArgs[0] : DefaultClonePath;
        var baselinePath = positionalArgs.Length > 1 ? positionalArgs[1] : DefaultBaselinePath();

        if (!Directory.Exists(clonePath))
        {
            Console.WriteLine(
                $"BSData clone not found at '{clonePath}' - pass a path as the first argument, or clone the live BSData 11th-edition repo to that location.");
            return 1;
        }

        var baseline = RuleClassificationBaseline.Load(baselinePath);

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

        // A text with a baseline entry is diverted entirely from the three sections below into its
        // own "Changed since verified" listing (task 4.3) - the baseline only ever suppresses what it
        // has actually recorded, so every other text keeps appearing exactly as before the baseline
        // existed.
        var unbaselined = results.Where(r => !baseline.TryGet(r.Text, out _)).ToList();
        var baselined = results
            .Where(r => baseline.TryGet(r.Text, out _))
            .Select(r => (Result: r,
                Diff: RuleClassificationDiff.Compare(baseline.Entries[r.Text].Classification, r.Classification)))
            .ToList();

        // Three-way split, not the spec's original two-way non-default/default-only: results with
        // 1+ Effects are their own section (the one worth eyeballing to confirm an extracted Effect
        // actually matches what the text says), separate from results whose Target is broader than
        // Self but which extracted no Effect at all (Templar Vows/Faith-Fuelled Resolve-shaped -
        // still a "non-default" classification per the spec, just not an Effect-review candidate).
        // An earlier version of this split (Effects-only vs. everything-else) silently dropped the
        // Target-only bucket from BOTH sections - caught live, not by any test.
        var effectResults = unbaselined
            .Where(r => r.Classification.Effects.Count > 0)
            .OrderBy(r => r.Names[0], StringComparer.OrdinalIgnoreCase)
            .ToList();

        var targetOnlyResults = unbaselined
            .Where(r => r.Classification.Target is not SelfRuleTarget && r.Classification.Effects.Count == 0)
            .OrderBy(r => r.Names[0], StringComparer.OrdinalIgnoreCase)
            .ToList();

        var defaultOnly = unbaselined
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

        var unchanged = baselined.Where(b => b.Diff.Status == RuleClassificationBaselineStatus.Unchanged).ToList();
        var changed = baselined
            .Where(b => b.Diff.Status != RuleClassificationBaselineStatus.Unchanged)
            .OrderBy(b => b.Result.Names[0], StringComparer.OrdinalIgnoreCase)
            .ToList();
        var fullyHandledCount = baseline.Entries.Values.Count(e => e.FullyHandled);

        Console.WriteLine();
        Console.WriteLine(
            $"=== Verified baseline: {baseline.Entries.Count} tracked, {unchanged.Count} unchanged, " +
            $"{changed.Count} changed since verified, {fullyHandledCount} fully handled ===");
        foreach (var (r, diff) in changed)
        {
            var kind = diff.Status == RuleClassificationBaselineStatus.Drift ? "DRIFT" : "NEW INFO";
            Console.WriteLine($"- [{kind}] {DescribeNames(r.Names)}");
            Console.WriteLine($"    text: \"{Truncate(r.Text)}\"");
            foreach (var change in diff.Changes)
            {
                Console.WriteLine(
                    $"    {change.Field}: baseline={change.BaselineValue?.ToJsonString() ?? "null"} -> current={change.CurrentValue?.ToJsonString() ?? "null"}");
            }

            if (baseline.Entries[r.Text].FullyHandled)
                Console.WriteLine("    [FULLY HANDLED] - reviewed, no further reading needed despite IsCaveated");
            if (baseline.Entries[r.Text].Note is { } note)
                Console.WriteLine($"    note: {note}");
            var extra = r.Locations.Count > 3 ? $", +{r.Locations.Count - 3} more" : "";
            Console.WriteLine($"    seen on: {string.Join("; ", r.Locations.Take(3))}{extra}");
        }

        // A baselined text's own IsCaveated says "this text states more than Target/Effects
        // captured" - a standing question ("extend the classifier, or accept this?") that a `note`
        // is the only thing that actually answers. Without this section, a caveated-and-unchanged
        // entry silently vanishes into the "unchanged" collapse above forever, with nothing ever
        // prompting a return visit - this list is that prompt, independent of Drift/NewInformation
        // status (a caveated entry with no note still needs a decision even when nothing else about
        // it changed this run). Once reviewed, adding a `note` to the entry is what removes it from
        // this list on the next run - the same mechanism the five originally-seeded caveated entries
        // already used, just not previously required for every future caveated entry too.
        var caveatedNeedingReview = baselined
            .Where(b => b.Result.Classification.IsCaveated
                        && baseline.Entries[b.Result.Text].Note is null
                        && !baseline.Entries[b.Result.Text].FullyHandled)
            .OrderBy(b => b.Result.Names[0], StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine();
        Console.WriteLine(
            $"=== Caveated baseline entries needing review (no note recorded yet): {caveatedNeedingReview.Count} ===");
        foreach (var (r, _) in caveatedNeedingReview)
        {
            var namesLabel = DescribeNames(r.Names);
            var extra = r.Locations.Count > 3 ? $", +{r.Locations.Count - 3} more" : "";
            Console.WriteLine($"- {namesLabel} -> {Describe(r.Classification)}");
            Console.WriteLine($"    text: \"{Truncate(r.Text)}\"");
            Console.WriteLine($"    seen on: {string.Join("; ", r.Locations.Take(3))}{extra}");
        }

        if (!writeBaseline)
            return 0;

        // Only refreshes entries already tracked in the baseline (matched by Text against this run's
        // corpus) - a genuinely new entry must first be added to the checked-in JSON by hand (at
        // minimum its Text) before this can snapshot a real classification onto it. Names is refreshed
        // here too - unlike Note/FullyHandled, it's a corpus-derived fact (which abilities currently
        // carry this text), not a human judgment call, so it belongs alongside Target/Effects/
        // IsCaveated. Note/FullyHandled are preserved untouched - a fresh classification run can't
        // derive either by itself.
        foreach (var (r, _) in baselined)
        {
            var existing = baseline.Entries[r.Text];
            baseline.Upsert(existing with
            {
                Target = r.Classification.Target,
                Effects = r.Classification.Effects,
                Names = r.Names,
                IsCaveated = r.Classification.IsCaveated
            });
        }

        baseline.Save(baselinePath);
        Console.WriteLine();
        Console.WriteLine($"--write-baseline: refreshed {baselined.Count} baseline entries at '{baselinePath}'.");

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

        var caveated = classification.IsCaveated ? "; CAVEATED" : "";

        return $"Target={target}; Effects=[{effects}]{caveated}";
    }

    private sealed class TextOccurrence
    {
        public SortedSet<string> Names { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Locations { get; } = [];
    }
}