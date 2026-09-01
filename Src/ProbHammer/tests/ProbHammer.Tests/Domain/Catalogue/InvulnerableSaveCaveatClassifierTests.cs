using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class InvulnerableSaveCaveatClassifierTests
{
    [Fact]
    public void Bare_ranged_template_resolves_rangedOnly()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveBare(
            "This model has a 4+ invulnerable save against ranged attacks.");

        result.Should().Be((Melee: 0, Ranged: 4));
    }

    [Fact]
    public void Bare_melee_template_resolves_meleeOnly()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveBare(
            "This model has a 5+ invulnerable save against melee attacks.");

        result.Should().Be((Melee: 5, Ranged: 0));
    }

    [Fact]
    public void Bare_unitScoped_ranged_template_resolves_rangedOnly()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveBare(
            "Models in this unit have a 4+ invulnerable save against ranged attacks.");

        result.Should().Be((Melee: 0, Ranged: 4));
    }

    [Fact]
    public void Bare_unitScoped_melee_template_resolves_meleeOnly()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveBare(
            "Models in this unit have a 4+ invulnerable save against melee attacks.");

        result.Should().Be((Melee: 4, Ranged: 0));
    }

    [Fact]
    public void Bare_unmatchedText_returnsNull()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveBare(
            "While a friendly model is on the battlefield, you cannot re-roll save rolls for this unit.");

        result.Should().BeNull();
    }

    [Fact]
    public void Bare_matchIgnoresRawFootnoteDigit_sinceThereIsNoneToCompareAgainst()
    {
        // TryResolveBare has no footnotedDigit parameter at all - the template's own value always
        // wins for the bare shape, regardless of whatever digit the raw "N+*" text carried.
        var result = InvulnerableSaveCaveatClassifier.TryResolveBare(
            "This model has a 4+ invulnerable save against ranged attacks.");

        result.Should().Be((Melee: 0, Ranged: 4));
    }

    [Fact]
    public void Split_matchingTemplate_consistentDigit_resolvesMatchedSidePlusPlainSide()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveSplit(
            "Models in this unit have a 4+ invulnerable save against melee attacks.",
            footnotedDigit: 4, plainDigit: 5);

        result.Should().Be((Melee: 4, Ranged: 5));
    }

    [Fact]
    public void Split_matchingTemplate_rangedSide_resolvesMatchedSidePlusPlainSide()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveSplit(
            "This model has a 4+ invulnerable save against ranged attacks.",
            footnotedDigit: 4, plainDigit: 5);

        result.Should().Be((Melee: 5, Ranged: 4));
    }

    [Fact]
    public void Split_digitMismatch_returnsNull()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveSplit(
            "Models in this unit have a 4+ invulnerable save against melee attacks.",
            footnotedDigit: 5, plainDigit: 3);

        result.Should().BeNull();
    }

    [Fact]
    public void Split_unmatchedText_returnsNull()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveSplit(
            "This save cannot be improved by any means.", footnotedDigit: 4, plainDigit: 5);

        result.Should().BeNull();
    }

    [Fact]
    public void ExtraLeadingContent_preventsResolution()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveBare(
            "While in Devastator Doctrine, this model has a 4+ invulnerable save against ranged attacks.");

        result.Should().BeNull();
    }

    [Fact]
    public void ExtraTrailingContent_preventsResolution()
    {
        var result = InvulnerableSaveCaveatClassifier.TryResolveBare(
            "This model has a 4+ invulnerable save against ranged attacks. This save cannot be improved.");

        result.Should().BeNull();
    }
}
