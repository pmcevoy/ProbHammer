using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Regression coverage for resolve-enhancement-abilities: BsdataDatasheetMapper must
/// distinguish an intrinsic, always-true datasheet ability from an optional, player-selectable
/// one (an Enhancement, or any other ability grant nested inside its own "type: upgrade" selection
/// entry) - see design.md's "Classification signal" and "Ability.Origin" decisions.</summary>
public class AbilityClassificationTests
{
    private static Datasheet Build()
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "ability-classification.json");
        var entry = BsdataNameResolver.Resolve(closure, "Test Unit")!;
        var idIndex = BsdataNameResolver.BuildIdIndex(closure);
        var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
        return BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex);
    }

    [Fact]
    public void AnEntryDirectAbility_IsExtractedAsIntrinsic()
    {
        var sheet = Build();

        sheet.Abilities.Should().ContainSingle(a => a.Name == "Intrinsic Ability" && a.Origin == AbilityOrigin.Intrinsic);
    }

    [Fact]
    public void AnAbilityNestedInAnEnhancementsGroup_IsExtractedAsAnEnhancement_NotIntrinsic()
    {
        var sheet = Build();

        sheet.Abilities.Should().NotContain(a => a.Name == "Some Enhancement");
        sheet.TryResolveAbility("Some Enhancement", out var ability).Should().BeTrue();
        ability.Origin.Should().Be(AbilityOrigin.Enhancement);
    }

    [Fact]
    public void AnAbilityNestedInANonEnhancementGroup_IsExtractedAsAPlainOptionalGrant()
    {
        var sheet = Build();

        sheet.Abilities.Should().NotContain(a => a.Name == "Some Grant");
        sheet.TryResolveAbility("Some Grant", out var ability).Should().BeTrue();
        ability.Origin.Should().Be(AbilityOrigin.OptionalGrant);
    }

    [Fact]
    public void AnEnhancementReachedThroughANestedSubPoolGroup_IsStillClassifiedAsAnEnhancement()
    {
        // "Legends of Saga and Song Enhancements" contains "Enhancements" as a substring but isn't
        // an exact match - confirmed necessary against the real corpus shape (see design.md).
        var sheet = Build();

        sheet.TryResolveAbility("Nested Enhancement", out var ability).Should().BeTrue();
        ability.Origin.Should().Be(AbilityOrigin.Enhancement);
    }
}
