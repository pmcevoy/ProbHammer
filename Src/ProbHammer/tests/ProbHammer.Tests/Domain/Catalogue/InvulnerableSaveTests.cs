using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class InvulnerableSaveTests
{
    [Fact]
    public void Default_IsAbsent()
    {
        var save = new InvulnerableSave();

        save.MeleeInSv.Should().Be(0);
        save.RangedInSv.Should().Be(0);
        save.Caveated.Should().BeFalse();
        save.CaveatAbility.Should().BeNull();
    }

    [Fact]
    public void Uniform_HasEqualMeleeAndRangedValues()
    {
        var save = new InvulnerableSave(4, 4, caveated: false, caveatAbility: null);

        save.MeleeInSv.Should().Be(4);
        save.RangedInSv.Should().Be(4);
        save.Caveated.Should().BeFalse();
        save.CaveatAbility.Should().BeNull();
    }

    [Fact]
    public void Caveated_WithAbility_IsAllowed()
    {
        var ability = new Ability { Name = "Invulnerable Save (5+*)", Text = "...", Scope = AbilityScope.Model, Origin = AbilityOrigin.Intrinsic };

        var save = new InvulnerableSave(5, 5, caveated: true, caveatAbility: ability);

        save.Caveated.Should().BeTrue();
        save.CaveatAbility.Should().BeSameAs(ability);
    }

    [Fact]
    public void Caveated_WithoutAbility_Throws()
    {
        var act = () => new InvulnerableSave(5, 5, caveated: true, caveatAbility: null);

        act.Should().Throw<ArgumentException>();
    }
}
