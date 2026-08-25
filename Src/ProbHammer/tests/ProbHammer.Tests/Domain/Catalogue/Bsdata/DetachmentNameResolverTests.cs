using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Covers army-roster-enrichment's Detachment Name Resolution requirement - the greedy,
/// longest-known-name-first "chomp" that disambiguates a natural-language-joined Detachments entry
/// without a syntactic split on "and"/commas, against
/// tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/detachment-resolution.json (shared with
/// <see cref="DetachmentRuleTextExtractorTests"/>). Lives alongside the other Bsdata-fixture-backed
/// tests (not Domain/Roster/) so BsdataFixtures.Source()'s own CallerFilePath-relative path
/// resolution reaches the fixtures directory correctly - the same reason
/// <see cref="ArmyRosterEnricherTests"/> (also Domain.Roster logic) lives here too.</summary>
public class DetachmentNameResolverTests
{
    private static ResolvedBsdataCatalogue Catalogue() =>
        ResolvedBsdataCatalogue.Build(BsdataFixtures.Source(), "detachment-resolution.json");

    [Fact]
    public void ASingleName_ResolvesDirectly()
    {
        var result = DetachmentNameResolver.Resolve("Marshal's Household", Catalogue());

        result.Should().ContainSingle().Which.Name.Should().Be("Marshal's Household");
    }

    [Fact]
    public void ANameContainingAnd_ResolvesAsOneWholeEntry_NeverSplit()
    {
        var result = DetachmentNameResolver.Resolve("Legends of Saga and Song", Catalogue());

        result.Should().ContainSingle().Which.Name.Should().Be("Legends of Saga and Song");
    }

    [Fact]
    public void ALongerNameIsNotSpuriouslySplitToAlsoClaimAContainedShorterName()
    {
        var result = DetachmentNameResolver.Resolve("Armoured Warhost", Catalogue());

        result.Should().ContainSingle().Which.Name.Should().Be("Armoured Warhost");
    }

    [Fact]
    public void TwoItemJoinedEntry_ResolvesBoth_InOrder()
    {
        var result = DetachmentNameResolver.Resolve(
            "Cabal of Chaos and Devotees of Destruction", Catalogue());

        result.Select(d => d.Name).Should().Equal("Cabal of Chaos", "Devotees of Destruction");
    }

    [Fact]
    public void ThreeItemOxfordCommaJoinedEntry_ResolvesAll_InOrder()
    {
        var result = DetachmentNameResolver.Resolve(
            "Fulguris Task Force, Marshal's Household, and Subversion Assets", Catalogue());

        result.Select(d => d.Name).Should().Equal("Fulguris Task Force", "Marshal's Household", "Subversion Assets");
    }

    [Fact]
    public void AnUnresolvableRemainder_FailsWithADiagnostic()
    {
        var act = () => DetachmentNameResolver.Resolve("Nonexistent Detachment", Catalogue());

        act.Should().Throw<BsdataNameResolutionException>().Which.Text.Should().Be("Nonexistent Detachment");
    }

    [Fact]
    public void AResolvedDetachmentWithZeroRules_CarriesAnEmptyRuleList()
    {
        var result = DetachmentNameResolver.Resolve("Warhost", Catalogue());

        result.Should().ContainSingle().Which.Rules.Should().BeEmpty();
    }
}