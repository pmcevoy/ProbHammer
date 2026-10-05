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
        new("Power sword", A: 3, Ws: 3, S: 4, Ap: -1, D: 1) { KeywordsText = ["Lethal Hits"] };

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
        result.KeywordsText.Should().Equal(weapon.KeywordsText);
    }

    [Fact]
    public void ResolvingAnAttacksCharacteristicEffect_Throws()
    {
        var weapon = PlainMeleeWeapon();
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "A", EffectVerb.Improve, 1);

        var act = () => WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, weapon);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static RangedWeapon PlainRangedWeapon(int bs = 4) =>
        new("Boltgun", Range: 24, A: 2, Bs: bs, S: 4, Ap: 0, D: 1);

    [Fact]
    public void BallisticSkillImprove_LowersARangedWeaponsSkill()
    {
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "BS", EffectVerb.Improve, 1);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, PlainRangedWeapon());

        result.Skill.Value.Should().Be((CharacteristicValue)3);
        result.Skill.OriginalValue.Should().Be((CharacteristicValue)4);
        result.Skill.ContributingAbilities.Should().Equal(SomeAbility);
    }

    [Fact]
    public void WeaponSkillWorsen_RaisesAMeleeWeaponsSkill()
    {
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "WS", EffectVerb.Worsen, 1);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, PlainMeleeWeapon());

        result.Skill.Value.Should().Be((CharacteristicValue)4);
    }

    [Fact]
    public void SkillResult_IsClampedAtTwo()
    {
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "BS", EffectVerb.Improve, 2);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, PlainRangedWeapon(bs: 3));

        result.Skill.Value.Should().Be((CharacteristicValue)2);
    }

    [Fact]
    public void SetSkillEffect_ReplacesTheValue()
    {
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "BS", EffectVerb.Set, 3);

        var result = WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, PlainRangedWeapon());

        result.Skill.Value.Should().Be((CharacteristicValue)3);
    }

    [Fact]
    public void ResolvingASkillEffectAgainstTheOtherWeaponType_Throws()
    {
        var effect = new WeaponCharacteristicEffect(new AllWeapons(), "WS", EffectVerb.Improve, 1);

        var act = () => WeaponCharacteristicEffectResolver.Resolve(effect, SomeAbility, PlainRangedWeapon());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("BS", false)]
    [InlineData("WS", true)]
    [InlineData("S", true)]
    public void SkillEffectApplies_ForAMeleeWeapon(string characteristic, bool expected) =>
        WeaponCharacteristicEffectResolver.SkillEffectApplies(characteristic, PlainMeleeWeapon())
            .Should().Be(expected);

    [Fact]
    public void SkillEffect_DoesNotApplyToAWeaponWithNoSkill()
    {
        var torrent = PlainRangedWeapon(bs: 0);

        WeaponCharacteristicEffectResolver.SkillEffectApplies("BS", torrent).Should().BeFalse();
    }
}
