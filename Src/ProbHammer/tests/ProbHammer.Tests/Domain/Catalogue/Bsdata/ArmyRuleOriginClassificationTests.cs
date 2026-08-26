using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Regression coverage for classify-known-army-rules: catalogue-json-ingestion's "Core
/// Versus Army Rule Origin Classification" is now driven entirely by a caller-supplied
/// `knownArmyRuleNames` set (via <see cref="ArmyRuleNameLookup"/>), never by whether the
/// referenced rule's own gating happens to carry a "primary-catalogue" condition - that structural
/// shape is also used, in the real BSData corpus, for mustering/composition rules (e.g. Assigned
/// Agents) that are not army-wide gameplay rules at all. See <c>PrimaryCatalogueGatingTests</c>
/// for the chapter-gating-specific coverage this complements.</summary>
public class ArmyRuleOriginClassificationTests
{
    private static Datasheet BuildFromCoreRuleAbilityFixture(IReadOnlySet<string>? knownArmyRuleNames = null)
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "core-rule-ability.json");
        var entry = BsdataNameResolver.Resolve(closure, "Test Unit")!;
        var idIndex = BsdataNameResolver.BuildIdIndex(closure);
        var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
        var glossary = RuleGlossary.Build(closure);
        return BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, glossary: glossary,
            knownArmyRuleNames: knownArmyRuleNames);
    }

    [Fact]
    public void ARuleMatchingTheCuratedLookup_HasArmyRuleOrigin_EvenWithNoStructuralGating()
    {
        // "Oath of Moment" in this fixture carries no primary-catalogue gating at all - Origin
        // must still be ArmyRule purely from the name match, mirroring real ungated army rules
        // like Death Guard's Nurgle's Gift.
        var sheet = BuildFromCoreRuleAbilityFixture(new HashSet<string> { "Oath of Moment" });

        sheet.Abilities.Single(a => a.Name == "Oath of Moment").Origin.Should().Be(AbilityOrigin.ArmyRule);
    }

    [Fact]
    public void ARuleNotMatchingTheCuratedLookup_HasCoreRuleOrigin()
    {
        var sheet = BuildFromCoreRuleAbilityFixture(new HashSet<string> { "Some Other Rule" });

        sheet.Abilities.Single(a => a.Name == "Oath of Moment").Origin.Should().Be(AbilityOrigin.CoreRule);
    }

    [Fact]
    public void AnAlliedUnitsRule_IsUnaffectedByADifferentFactionsCuratedEntry()
    {
        // Mirrors an allied Imperial Knight inside a Custodes roster: ArmyRosterEnricher resolves
        // one knownArmyRuleNames set from the roster's own primary Faction and supplies it to
        // every unit's Datasheet resolution, including an allied unit's own - so a name that only
        // matches a DIFFERENT faction's curated entry (here, Custodes' "Martial Ka'tah") has no
        // bearing on this unit's own "Oath of Moment" reference.
        var custodesNames = ArmyRuleNameLookup.Resolve(["Adeptus Custodes"]);

        var sheet = BuildFromCoreRuleAbilityFixture(custodesNames);

        sheet.Abilities.Single(a => a.Name == "Oath of Moment").Origin.Should().Be(AbilityOrigin.CoreRule);
    }

    [Fact]
    public void WithNoKnownArmyRuleNamesSupplied_EveryReferenceClassifiesCoreRule()
    {
        // Every pre-existing BuildDatasheet call site that doesn't pass knownArmyRuleNames - fails
        // open, same convention as primaryCatalogueId/forceEntries.
        var sheet = BuildFromCoreRuleAbilityFixture();

        sheet.Abilities.Single(a => a.Name == "Oath of Moment").Origin.Should().Be(AbilityOrigin.CoreRule);
    }
}