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
        var view = new ScalarCharacteristicView(
            new NumericCharacteristicValue(4), null, [MakeAbility("Some Ability")]);

        view.IsCaveated.Should().BeTrue();
    }

    [Fact]
    public void Scalar_NullDerivedValueWithNoContributingAbilities_Throws()
    {
        var act = () => new ScalarCharacteristicView(new NumericCharacteristicValue(4), null, []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Scalar_NotCaveated_ValueIsDerivedValue()
    {
        var original = new NumericCharacteristicValue(4);
        var derived = new NumericCharacteristicValue(5);
        var view = new ScalarCharacteristicView(original, derived, []);

        view.Value.Should().Be(derived);
    }

    [Fact]
    public void Scalar_Caveated_ValueIsOriginalValue()
    {
        var original = new NumericCharacteristicValue(4);
        var view = new ScalarCharacteristicView(original, null, [MakeAbility("Some Ability")]);

        view.Value.Should().Be(original);
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

        var view = new InvulnerableSaveCharacteristicView(original, null, [MakeAbility("Some Ability")]);

        view.IsCaveated.Should().BeTrue();
    }

    [Fact]
    public void InvulnerableSave_NullDerivedValueWithNoContributingAbilities_Throws()
    {
        var act = () => new InvulnerableSaveCharacteristicView(5, null, []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvulnerableSave_NotCaveated_ValueIsDerivedValue()
    {
        InvulnerableSave original = 5;
        InvulnerableSave derived = 4;
        var view = new InvulnerableSaveCharacteristicView(original, derived, []);

        view.Value.Should().Be(derived);
    }

    [Fact]
    public void InvulnerableSave_Caveated_ValueIsOriginalValue()
    {
        InvulnerableSave original = 5;
        var view = new InvulnerableSaveCharacteristicView(original, null, [MakeAbility("Some Ability")]);

        view.Value.Should().Be(original);
    }

    [Fact]
    public void InvulnerableSave_Resolved_IsNotCaveatedWithNoContributingAbilities()
    {
        InvulnerableSave value = 5;

        var view = InvulnerableSaveCharacteristicView.Resolved(value);

        view.OriginalValue.Should().Be(value);
        view.DerivedValue.Should().Be(value);
        view.ContributingAbilities.Should().BeEmpty();
        view.IsCaveated.Should().BeFalse();
    }

    [Fact]
    public void InvulnerableSave_ResolvedWithContributingAbilities_IsNotCaveated()
    {
        InvulnerableSave value = 5;
        var ability = MakeAbility("Shield Dome");

        var view = InvulnerableSaveCharacteristicView.Resolved(value, [ability]);

        view.OriginalValue.Should().Be(value);
        view.DerivedValue.Should().Be(value);
        view.ContributingAbilities.Should().Equal(ability);
        view.IsCaveated.Should().BeFalse();
    }

    [Fact]
    public void InvulnerableSave_Caveated_HasNoDerivedValueAndOneContributingAbility()
    {
        InvulnerableSave original = 5;
        var ability = MakeAbility("Some Ability");

        var view = InvulnerableSaveCharacteristicView.Caveated(original, ability);

        view.OriginalValue.Should().Be(original);
        view.DerivedValue.Should().BeNull();
        view.ContributingAbilities.Should().Equal(ability);
        view.IsCaveated.Should().BeTrue();
    }

    [Fact]
    public void InvulnerableSave_None_IsTheAbsentValueWithNoContributingAbilities()
    {
        InvulnerableSaveCharacteristicView.None.OriginalValue.Should().Be(InvulnerableSave.None);
        InvulnerableSaveCharacteristicView.None.DerivedValue.Should().Be(InvulnerableSave.None);
        InvulnerableSaveCharacteristicView.None.ContributingAbilities.Should().BeEmpty();
        InvulnerableSaveCharacteristicView.None.IsCaveated.Should().BeFalse();
    }

    [Fact]
    public void Equality_ComparesContributingAbilitiesByContentNotByListReference()
    {
        // Regression: two views built from independently-constructed (but content-identical)
        // ContributingAbilities list instances must still compare equal - a plain compiler-generated
        // record Equals would compare the IReadOnlyList<Ability> field by reference and incorrectly
        // report these as different, breaking any consumer relying on structural equality (e.g.
        // LivePlayModel.GroupStatlines' run-merging logic).
        var ability = MakeAbility("Shield Dome");
        InvulnerableSave value = 5;

        var viewA = new InvulnerableSaveCharacteristicView(value, value, [ability]);
        var viewB = new InvulnerableSaveCharacteristicView(value, value, [ability]);

        viewA.Should().Be(viewB);
        viewA.GetHashCode().Should().Be(viewB.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentContributingAbilities_AreNotEqual()
    {
        InvulnerableSave value = 5;
        var viewA = new InvulnerableSaveCharacteristicView(value, value, [MakeAbility("Shield Dome")]);
        var viewB = new InvulnerableSaveCharacteristicView(value, value, [MakeAbility("Some Other Ability")]);

        viewA.Should().NotBe(viewB);
    }
}