using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Regression coverage for gate-and-dedupe-core-rule-abilities: a Core Rule reference's
/// target rule can carry its own chapter/sub-faction ("primary-catalogue") exclusivity - confirmed
/// real shapes: Oath of Moment hidden unless the army's primary catalogue is one of 11 named
/// chapters (Black Templars deliberately excluded); Templar Vows hidden unless it specifically is
/// Black Templars. Running the real pipeline against both a Black Templars and a Salamanders
/// closure before this fix produced the identical (and therefore wrong, for at least one of them)
/// ability list.</summary>
public class PrimaryCatalogueGatingTests
{
    private static Datasheet Build(
        string? primaryCatalogueId, IReadOnlySet<string>? knownArmyRuleNames = null)
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "primary-catalogue-gating.json");
        var entry = BsdataNameResolver.Resolve(closure, "Test Unit")!;
        var idIndex = BsdataNameResolver.BuildIdIndex(closure);
        var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
        var glossary = RuleGlossary.Build(closure);
        return BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, glossary: glossary,
            primaryCatalogueId: primaryCatalogueId, knownArmyRuleNames: knownArmyRuleNames);
    }

    [Fact]
    public void AChapterExclusiveVow_IsIncluded_ForItsOwnMatchingChapter()
    {
        var sheet = Build("cat-a");

        sheet.Abilities.Select(a => a.Name).Should().Contain("Chapter Vow");
    }

    [Fact]
    public void AChapterExclusiveVow_IsExcluded_ForADifferentChapter()
    {
        var sheet = Build("cat-b");

        sheet.Abilities.Select(a => a.Name).Should().NotContain("Chapter Vow");
    }

    [Fact]
    public void AnInstanceOfExclusion_IsExcluded_ForItsOwnMatchingChapter()
    {
        var sheet = Build("cat-a");

        sheet.Abilities.Select(a => a.Name).Should().NotContain("Excluded For A");
    }

    [Fact]
    public void AnInstanceOfExclusion_IsIncluded_ForADifferentChapter()
    {
        var sheet = Build("cat-b");

        sheet.Abilities.Select(a => a.Name).Should().Contain("Excluded For A");
    }

    [Fact]
    public void AnUnrecognizedGatingShape_NeverExcludesTheReference()
    {
        var sheet = Build("cat-a");

        sheet.Abilities.Select(a => a.Name).Should().Contain("Unrecognized Gate");
    }

    [Fact]
    public void AChapterExclusiveVowMatchingTheCuratedLookup_HasArmyRuleOrigin()
    {
        // classify-known-army-rules: chapter/sub-faction gating is no longer itself the Origin
        // signal (the same shape is also used for real mustering rules like Assigned Agents) -
        // Origin is driven entirely by the caller-supplied knownArmyRuleNames set.
        var sheet = Build("cat-a", knownArmyRuleNames: new HashSet<string> { "Chapter Vow" });

        sheet.Abilities.Single(a => a.Name == "Chapter Vow").Origin.Should().Be(AbilityOrigin.ArmyRule);
    }

    [Fact]
    public void AChapterExclusiveVowNotMatchingTheCuratedLookup_HasPlainCoreRuleOrigin()
    {
        // The regression case classify-known-army-rules fixes: a rule carrying primary-catalogue
        // gating (the Assigned-Agents shape) is no longer promoted to ArmyRule on that structural
        // signal alone.
        var sheet = Build("cat-a");

        sheet.Abilities.Single(a => a.Name == "Chapter Vow").Origin.Should().Be(AbilityOrigin.CoreRule);
    }

    [Fact]
    public void AnUngatedRule_HasPlainCoreRuleOrigin()
    {
        var sheet = Build("cat-a");

        sheet.Abilities.Single(a => a.Name == "Ungated Rule").Origin.Should().Be(AbilityOrigin.CoreRule);
    }

    [Fact]
    public void AnUngatedRule_IsAlwaysIncluded_RegardlessOfChapter()
    {
        Build("cat-a").Abilities.Select(a => a.Name).Should().Contain("Ungated Rule");
        Build("cat-b").Abilities.Select(a => a.Name).Should().Contain("Ungated Rule");
    }

    [Fact]
    public void WithNoPrimaryCatalogueIdSupplied_NothingIsGated()
    {
        // Every pre-existing BuildDatasheet call site that doesn't pass primaryCatalogueId - fails
        // open, same convention as GatedContent_IsIncluded_WhenNoForceEntriesAreProvided.
        var sheet = Build(primaryCatalogueId: null);

        sheet.Abilities.Select(a => a.Name).Should()
            .Contain(["Chapter Vow", "Excluded For A", "Unrecognized Gate", "Ungated Rule"]);
    }
}