using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Regression coverage for resolve-core-rule-abilities: a "type: rule" infoLink (BSData's
/// shape for a datasheet-wide Core or faction rule reference - Oath of Moment, Templar Vows,
/// Deadly Demise D3, Firing Deck 6, Infiltrators, Scouts 6") must resolve into an always-exposed
/// Ability with Origin CoreRule, not silently dropped (the bug this change fixes) and not routed
/// through the on-demand optional-ability index (Core rules are never a player's optional
/// selection - see design.md).</summary>
public class CoreRuleAbilityTests
{
    private static Datasheet Build()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "core-rule-ability.json");
        var entry = BsdataNameResolver.Resolve(closure, "Test Unit")!;
        var idIndex = BsdataNameResolver.BuildIdIndex(closure);
        var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
        var glossary = RuleGlossary.Build(closure);
        return BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, glossary: glossary);
    }

    [Fact]
    public void AStringValuedAppendModifier_ProducesTheAppendedDisplayName()
    {
        var sheet = Build();

        sheet.Abilities.Should().ContainSingle(a => a.Name == "Deadly Demise D3" && a.Origin == AbilityOrigin.CoreRule);
    }

    [Fact]
    public void ANumberValuedAppendModifier_ProducesTheAppendedDisplayName()
    {
        var sheet = Build();

        sheet.Abilities.Should().ContainSingle(a => a.Name == "Firing Deck 6" && a.Origin == AbilityOrigin.CoreRule);
    }

    [Fact]
    public void ARuleReferenceWithNoAppendModifier_KeepsTheBareName()
    {
        var sheet = Build();

        sheet.Abilities.Should().ContainSingle(a => a.Name == "Oath of Moment" && a.Origin == AbilityOrigin.CoreRule);
    }

    [Fact]
    public void TheResolvedTextComesFromTheReferencedRuleDefinition()
    {
        var sheet = Build();

        sheet.Abilities.Single(a => a.Name == "Oath of Moment").Text
            .Should().Contain("Adeptus Astartes");
    }

    [Fact]
    public void AnUnresolvableRuleReference_IsSilentlySkipped()
    {
        var sheet = Build();

        sheet.Abilities.Should().NotContain(a => a.Name.Contains("Nonexistent"));
    }

    [Fact]
    public void ARuleReferenceNestedInAWeaponOptionEntry_IsExcluded_NotADatasheetWideAbility()
    {
        // Real-corpus regression: the Impulsor's "Ironhail Skytalon Array" weapon-option entry
        // carries its own "Sustained Hits"/"Anti" rule infoLinks describing that weapon's own
        // Keywords characteristic - a "type: upgrade" ancestor means this is a weapon-scoped
        // cross-reference, not a datasheet-wide Core rule, even though it resolves against the
        // glossary just as successfully as a genuine one would.
        var sheet = Build();

        sheet.Abilities.Should().NotContain(a => a.Name == "Sustained Hits");
        sheet.OptionalAbilityNames.Should().NotContain("Sustained Hits");
    }

    [Fact]
    public void ACoreRuleAbility_IsScopedToUnit()
    {
        var sheet = Build();

        sheet.Abilities.Single(a => a.Name == "Oath of Moment").Scope.Should().Be(AbilityScope.Unit);
    }

    [Fact]
    public void ACoreRuleAbility_NeverAppearsInTheOnDemandOptionalIndex()
    {
        var sheet = Build();

        sheet.OptionalAbilityNames.Should().NotContain("Oath of Moment");
        sheet.TryResolveAbility("Oath of Moment", out _).Should().BeFalse();
    }

    [Fact]
    public void WithNoGlossaryPassed_NoCoreRuleAbilitiesAreExtracted()
    {
        // Every pre-existing BuildDatasheet call site (tests, any caller that doesn't pass a
        // glossary) must keep compiling and behaving unchanged - see design.md's "new optional
        // RuleGlossary? parameter, default null" decision.
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "core-rule-ability.json");
        var entry = BsdataNameResolver.Resolve(closure, "Test Unit")!;
        var idIndex = BsdataNameResolver.BuildIdIndex(closure);
        var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);

        var sheet = BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex);

        sheet.Abilities.Should().BeEmpty();
    }
}
