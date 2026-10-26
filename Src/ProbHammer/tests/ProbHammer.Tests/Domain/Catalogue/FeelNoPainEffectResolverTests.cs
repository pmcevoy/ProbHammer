using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class FeelNoPainEffectResolverTests
{
    private static readonly Ability Grant = Ability("Some Grant");
    private static readonly Ability Own = Ability("Feel No Pain 5+");

    private static Ability Ability(string name) => new()
    {
        Name = name,
        Text = $"{name} text.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.CoreRule
    };

    private static readonly ScalarCharacteristicView OwnFivePlus =
        ScalarCharacteristicView.Resolved(Statline.NoFeelNoPain.OriginalValue, 5, [Own]);

    [Fact]
    public void AStatlineHasNoFeelNoPainByDefault()
    {
        var fnp = new Statline(6, 4, 3, 2, 6, 2).Fnp;

        FeelNoPainEffectResolver.Threshold(fnp).Should().BeNull();
        fnp.IsCaveated.Should().BeFalse();
        fnp.ContributingAbilities.Should().BeEmpty();
    }

    [Fact]
    public void TwoStatlinesDifferingOnlyInFeelNoPain_AreNotEqual()
    {
        var plain = new Statline(6, 4, 3, 2, 6, 2);

        (plain with { Fnp = OwnFivePlus }).Should().NotBe(plain);
    }

    [Fact]
    public void AGrantToAnAbsentValue_TakesTheGrant()
    {
        var result = FeelNoPainEffectResolver.Merge(new FeelNoPainEffect(4), Grant, Statline.NoFeelNoPain);

        FeelNoPainEffectResolver.Threshold(result).Should().Be(4);
        result.OriginalValue.Should().Be(Statline.NoFeelNoPain.OriginalValue);
        result.ContributingAbilities.Should().Equal(Grant);
    }

    [Fact]
    public void ABetterGrant_ReplacesTheExistingSource()
    {
        var result = FeelNoPainEffectResolver.Merge(new FeelNoPainEffect(4), Grant, OwnFivePlus);

        FeelNoPainEffectResolver.Threshold(result).Should().Be(4);
        result.OriginalValue.Should().Be(Statline.NoFeelNoPain.OriginalValue);
        result.ContributingAbilities.Should().Equal(Grant);
    }

    [Fact]
    public void AWorseGrant_LeavesTheValueAlone()
    {
        var result = FeelNoPainEffectResolver.Merge(new FeelNoPainEffect(6), Grant, OwnFivePlus);

        result.Should().BeSameAs(OwnFivePlus);
    }
}
