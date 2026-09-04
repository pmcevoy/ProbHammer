using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class CharacteristicViewTests
{
    private static Ability MakeAbility(string name) => new()
    {
        Name = name,
        Text = "...",
        Scope = AbilityScope.Unit,
        Origin = AbilityOrigin.Intrinsic
    };

    [Fact]
    public void Scalar_RoundTripsOriginalValueDerivedValueAndContributingAbilities()
    {
        var original = new NumericCharacteristicValue(4);
        var derived = new NumericCharacteristicValue(5);
        var contributor = MakeAbility("Some Ability");

        var view = new ScalarCharacteristicView(original, derived, [contributor]);

        view.OriginalValue.Should().Be(original);
        view.DerivedValue.Should().Be(derived);
        view.ContributingAbilities.Should().Equal(contributor);
    }

    [Fact]
    public void Scalar_NonNullDerivedValue_IsNotCaveated()
    {
        var view = new ScalarCharacteristicView(
            new NumericCharacteristicValue(4), new NumericCharacteristicValue(4), []);

        view.IsCaveated.Should().BeFalse();
    }

    [Fact]
    public void Scalar_NullDerivedValue_IsCaveated()
    {
        var view = new ScalarCharacteristicView(new NumericCharacteristicValue(4), null, []);

        view.IsCaveated.Should().BeTrue();
    }

    [Fact]
    public void InvulnerableSave_RoundTripsOriginalValueDerivedValueAndContributingAbilities()
    {
        InvulnerableSave original = 5;
        InvulnerableSave derived = 4;
        var contributor = MakeAbility("Some Ability");

        var view = new InvulnerableSaveCharacteristicView(original, derived, [contributor]);

        view.OriginalValue.Should().Be(original);
        view.DerivedValue.Should().Be(derived);
        view.ContributingAbilities.Should().Equal(contributor);
    }

    [Fact]
    public void InvulnerableSave_NonNullDerivedValue_IsNotCaveated()
    {
        InvulnerableSave value = 5;

        var view = new InvulnerableSaveCharacteristicView(value, value, []);

        view.IsCaveated.Should().BeFalse();
    }

    [Fact]
    public void InvulnerableSave_NullDerivedValue_IsCaveated()
    {
        InvulnerableSave original = 5;

        var view = new InvulnerableSaveCharacteristicView(original, null, []);

        view.IsCaveated.Should().BeTrue();
    }
}