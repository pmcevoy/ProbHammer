using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

public class WeaponCharacteristicEffectResolverTests
{
    private static readonly Ability SomeAbility = new()
    {
        Name = "Some Ability",
        Text = "Irrelevant to this resolver - only Name/Text identity matters here.",
        Scope = AbilityScope.Model,
        Origin = AbilityOrigin.OptionalGrant
    };

    private static MeleeWeapon PlainMeleeWeapon() =>
        new("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1) { LethalHits = true };

    [Fact]
    public void ImproveEffect_RaisesTheTargetedCharacteristic()
    {
        var weapon = PlainMeleeWeapon();
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, weapon);

        result.S.IsCaveated.Should().BeFalse();
        result.S.Value.Should().Be((CharacteristicValue)5);
        result.S.ContributingAbilities.Should().Equal(SomeAbility);
    }

    [Fact]
    public void ArmourPenetrationEffect_FollowsTheNegativeIntegerSignConvention()
    {
        var weapon = PlainMeleeWeapon(); // Ap: -1
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "AP", EffectVerb.Improve, 1);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, weapon);

        result.Ap.Value.Should().Be((CharacteristicValue)(-2));
    }

    [Fact]
    public void DamageEffect_ResolvesDiceAware_PreservingTheDiceComponent()
    {
        var weapon = new MeleeWeapon("Chainsword", A: 4, Ws: 3, S: 4, Ap: 0, D: DiceExpression.D3);
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "D", EffectVerb.Improve, 1);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, weapon);

        var resolvedDice = ((DiceCharacteristicValue)result.D.Value).Value;
        resolvedDice.Count.Should().Be(DiceExpression.D3.Count);
        resolvedDice.Sides.Should().Be(DiceExpression.D3.Sides);
        resolvedDice.Modifier.Should().Be(1); // D3's own Modifier (0) + 1
    }

    [Fact]
    public void PreMutationOriginalValue_IsPreservedFromTheFieldsOwnOriginal_NotItsCurrentEffectiveValue()
    {
        // The field already carries one prior mutation (OriginalValue 3 -> DerivedValue 4) -
        // resolving a second effect must keep reporting the true pre-mutation 3, not silently
        // adopt 4 as if it were the original.
        var alreadyMutated = ScalarCharacteristicView.Resolved(
            originalValue: (CharacteristicValue)3, derivedValue: (CharacteristicValue)4, [SomeAbility]);
        var weapon = PlainMeleeWeapon() with { S = alreadyMutated };
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, weapon);

        result.S.OriginalValue.Should().Be((CharacteristicValue)3);
        result.S.DerivedValue.Should().Be((CharacteristicValue)5); // 4 (current effective) + 1
    }

    [Fact]
    public void EveryOtherFieldOfTheProfile_IsUnchangedByResolution()
    {
        var weapon = PlainMeleeWeapon();
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "S", EffectVerb.Improve, 1);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, weapon);

        result.Name.Should().Be(weapon.Name);
        result.Type.Should().Be(weapon.Type);
        result.Range.Should().Be(weapon.Range);
        result.A.Should().Be(weapon.A);
        result.Ap.Should().Be(weapon.Ap);
        result.D.Should().Be(weapon.D);
        result.LethalHits.Should().Be(weapon.LethalHits);
    }

    [Fact]
    public void ResolvingAnAttacksCharacteristicEffect_Throws()
    {
        var weapon = PlainMeleeWeapon();
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "A", EffectVerb.Improve, 1);

        var act = () => WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, weapon);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
