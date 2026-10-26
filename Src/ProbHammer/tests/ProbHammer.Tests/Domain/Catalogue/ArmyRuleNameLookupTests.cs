using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

/// <summary>Coverage for army-rule-name-lookup's Curated Per-Faction Army Rule Names and
/// Faction-Scoped Resolution requirements.</summary>
public class ArmyRuleNameLookupTests
{
    [Fact]
    public void AFactionWithAKnownArmyWideRule_ResolvesItsName()
    {
        ArmyRuleNameLookup.Resolve(["Death Guard"]).Should().Contain("Nurgle's Gift (Aura)");
    }

    [Fact]
    public void AFactionWithNoCuratedEntry_ResolvesToAnEmptySet()
    {
        ArmyRuleNameLookup.Resolve(["Some Unmapped Faction"]).Should().BeEmpty();
    }

    [Fact]
    public void ASubFaction_ResolvesAgainstItsOwnEntry_NotItsParentCodexs()
    {
        var names = ArmyRuleNameLookup.Resolve(["Imperium", "Adeptus Custodes"]);

        names.Should().Equal("Martial Ka'tah");
    }

    [Fact]
    public void AnUnsplitFaction_ResolvesDirectly()
    {
        ArmyRuleNameLookup.Resolve(["Death Guard"]).Should().Contain("Nurgle's Gift (Aura)");
    }
}