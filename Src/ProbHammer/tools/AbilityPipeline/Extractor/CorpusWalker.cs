using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tools.AbilityPipeline.Extractor;

/// <summary>One raw Name+Text occurrence collected during a corpus walk - many of these collapse
/// into a single <see cref="AbilityCorpusRecord"/> once grouped by normalized-text hash.</summary>
public readonly record struct RawOccurrence(string Name, string Text, string SourceKind, string Location);

/// <summary>Walks the entire BSData corpus once, reusing ProbHammer.Core's own corpus-resolution calls
/// (local + shared rules, always-enumerated <see cref="Datasheet.Abilities"/>, on-demand
/// Enhancement/OptionalGrant abilities, Detachment rule text) - no independent parsing of
/// <c>catalogueLinks</c>/<c>infoLink</c>/<c>infoGroup</c> structures, per this pipeline's own
/// design decision to reuse Domain.Catalogue.Bsdata rather than re-derive it.</summary>
public static class CorpusWalker
{
    public const string ExcludedFileName = "Warhammer 40,000.json";

    public static IEnumerable<RawOccurrence> Walk(string clonePath, TextWriter log, CorpusVocabulary vocabulary)
    {
        var source = new LocalDiskBsdataCatalogueSource(clonePath);
        var fileNames = source.ListFileNames()
            .Where(name => !string.Equals(name, ExcludedFileName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

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
                log.WriteLine($"  Skipping '{fileName}': closure resolution failed ({ex.Message})");
                continue;
            }

            var glossary = RuleGlossary.Build(closure);
            var idIndex = BsdataNameResolver.BuildIdIndex(closure);
            var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
            var profileIndex = BsdataNameResolver.BuildProfileIdIndex(closure);

            foreach (var rule in closure.Files[0].Catalogue.Rules)
            {
                CountRuleNames(vocabulary, rule.Name, rule.Alias);
                yield return new RawOccurrence(rule.Name, rule.Description, SourceKinds.RawSharedRule,
                    $"{fileName} :: rule '{rule.Name}'");
            }

            if (!scannedSharedRules && closure.GameSystem is not null)
            {
                scannedSharedRules = true;
                foreach (var rule in closure.GameSystem.SharedRules)
                {
                    CountRuleNames(vocabulary, rule.Name, rule.Alias);
                    yield return new RawOccurrence(rule.Name, rule.Description, SourceKinds.RawSharedRule,
                        $"(game system) :: shared rule '{rule.Name}'");
                }
            }

            foreach (var detachmentEntry in BsdataNameResolver.ResolveDetachmentEntries(closure))
            {
                foreach (var (name, text) in DetachmentRuleTextExtractor.Extract(detachmentEntry, glossary))
                    yield return new RawOccurrence(name, text, SourceKinds.DetachmentRule,
                        $"{fileName} :: detachment '{detachmentEntry.Name}' :: '{name}'");
            }

            foreach (var entry in closure.Files[0].Catalogue.SharedSelectionEntries)
            {
                Datasheet datasheet;
                try
                {
                    // glossary must be passed for a "type: rule" infoLink (Core Rule / Army Rule
                    // Ability Extraction) to resolve at all - ProcessRuleInfoLink no-ops on a null
                    // Glossary. knownArmyRuleNames uses the *global* union across every faction
                    // (ArmyRuleNameLookup.AllKnownNames), not one resolved Faction's own subset,
                    // since a corpus walk has no roster to resolve a Faction from - an approximation
                    // accepted here because these names are curated to be faction-specific in
                    // practice, and it's strictly better than the previous unconditional CoreRule.
                    datasheet = BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, profileIndex,
                        glossary: glossary, knownArmyRuleNames: ArmyRuleNameLookup.AllKnownNames);
                }
                catch
                {
                    // Characteristic-resolution failures are covered by CharacteristicResolutionScanTests -
                    // an entry that fails to build a Datasheet at all has no Ability.Text to collect here.
                    continue;
                }

                CountDatasheetNames(vocabulary, datasheet);

                foreach (var ability in datasheet.Abilities)
                    yield return new RawOccurrence(ability.Name, ability.Text, SourceKinds.ForOrigin(ability.Origin),
                        $"{fileName} :: '{entry.Name}' ability '{ability.Name}'");

                // A footnoted InSv's linked ability isn't in Abilities, but LivePlay resolves the
                // caveat through its classification.
                foreach (var (statlineName, statline) in datasheet.Statlines)
                {
                    if (!statline.InSv.IsCaveated)
                        continue;
                    foreach (var caveat in statline.InSv.ContributingAbilities)
                        yield return new RawOccurrence(caveat.Name, caveat.Text, SourceKinds.ForOrigin(caveat.Origin),
                            $"{fileName} :: '{entry.Name}' statline '{statlineName}' InSv caveat '{caveat.Name}'");
                }

                foreach (var optionalName in datasheet.OptionalAbilityNames)
                {
                    if (datasheet.TryResolveAbility(optionalName, out var ability))
                    {
                        yield return new RawOccurrence(ability.Name, ability.Text,
                            SourceKinds.ForOrigin(ability.Origin),
                            $"{fileName} :: '{entry.Name}' optional ability '{ability.Name}'");
                    }
                }
            }
        }
    }

    private static void CountRuleNames(CorpusVocabulary vocabulary, string name, IEnumerable<string> aliases)
    {
        CorpusVocabulary.Count(vocabulary.AbilityNames, name);
        foreach (var alias in aliases)
            CorpusVocabulary.Count(vocabulary.AbilityNames, alias);
    }

    private static void CountDatasheetNames(CorpusVocabulary vocabulary, Datasheet datasheet)
    {
        foreach (var keyword in datasheet.Keywords)
            CorpusVocabulary.Count(vocabulary.UnitKeywords, keyword);

        foreach (var ability in datasheet.Abilities)
            CorpusVocabulary.Count(vocabulary.AbilityNames, ability.Name);
        foreach (var optionalName in datasheet.OptionalAbilityNames)
            CorpusVocabulary.Count(vocabulary.AbilityNames, optionalName);

        foreach (var weaponName in datasheet.WeaponNames)
        {
            CorpusVocabulary.Count(vocabulary.WeaponNames, weaponName);
            foreach (var keyword in datasheet.ResolveWeaponProfile(weaponName).KeywordsText)
                CorpusVocabulary.Count(vocabulary.WeaponKeywords, keyword);
        }
    }
}