using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

/// <summary>Covers sign resolution per arithmetic family, per-characteristic clamp bounds,
/// symbolic-value no-op, and the proving requirement against Vexilla's own known-correct Objective
/// Control mutation - Shield Dome/InSv is not a valid proving example here since it's a
/// caveated invulnerable-save value, not a plain scalar mutation.</summary>
public class CharacteristicModificationResolverTests
{
    [Theory]
    [InlineData(CharacteristicModificationKind.RollThreshold, EffectVerb.Improve, 1, -1)]
    [InlineData(CharacteristicModificationKind.RollThreshold, EffectVerb.Worsen, 1, 1)]
    [InlineData(CharacteristicModificationKind.ArmourPenetration, EffectVerb.Improve, 1, -1)]
    [InlineData(CharacteristicModificationKind.ArmourPenetration, EffectVerb.Worsen, 1, 1)]
    [InlineData(CharacteristicModificationKind.Plain, EffectVerb.Improve, 1, 1)]
    [InlineData(CharacteristicModificationKind.Plain, EffectVerb.Worsen, 1, -1)]
    public void ResolveDelta_ResolvesTheSignedDeltaForEachFamilyAndVerb(
        CharacteristicModificationKind kind, EffectVerb verb, int amount, int expectedDelta)
    {
        CharacteristicModificationResolver.ResolveDelta(kind, verb, amount).Should().Be(expectedDelta);
    }

    [Fact]
    public void ResolveDelta_SetVerb_ThrowsRatherThanGuessAtASign()
    {
        var act = () => CharacteristicModificationResolver.ResolveDelta(
            CharacteristicModificationKind.Plain, EffectVerb.Set, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("AP", 1, 0)] // worsening AP past its cap of 0 clamps to 0
    [InlineData("Sv", 1, 2)] // improving Sv past its floor of 2+ clamps to 2+
    [InlineData("Ld", 3, 5)] // improving Ld past its floor of 5+ clamps to 5+
    [InlineData("Ld", 9, 8)] // worsening Ld past its ceiling of 8+ clamps to 8+
    [InlineData("WS", 1, 2)] // improving WS past its floor of 2+ clamps to 2+
    [InlineData("BS", 8, 6)] // worsening BS past its ceiling of 6+ clamps to 6+
    [InlineData("Oc", -2, 0)] // worsening Oc below 0 clamps to 0
    [InlineData("M", 0, 1)] // worsening Movement below 1" clamps to 1"
    public void Clamp_CapsAValueAtItsCharacteristicsBound(string characteristic, int value, int expected)
    {
        CharacteristicModificationClamp.Apply(characteristic, value).Should().Be(expected);
    }

    [Fact]
    public void Clamp_ACharacteristicWithNoRegisteredBound_PassesThroughUnchanged()
    {
        CharacteristicModificationClamp.Apply("W", -5).Should().Be(-5);
    }

    // The inverse function: raw stored-value delta -> rulebook verb, the mirror image of
    // ResolveDelta above. Auric Mantle's real corpus modifier is a structural "increment" of 2 on
    // Toughness's own Plain-family characteristic (W, in this Datasheet's own field allowlist) - a
    // positive raw delta on a Plain characteristic is Improve, sign carried straight through.
    [Fact]
    public void ResolveVerbFromRawDelta_PlainFamily_AuricMantle_PositiveDeltaIsImprove()
    {
        var (verb, amount) = CharacteristicModificationResolver.ResolveVerbFromRawDelta(
            CharacteristicModificationKind.Plain, delta: 2);

        verb.Should().Be(EffectVerb.Improve);
        amount.Should().Be(2);
    }

    [Fact]
    public void ResolveVerbFromRawDelta_PlainFamily_NegativeDeltaIsWorsen()
    {
        var (verb, amount) = CharacteristicModificationResolver.ResolveVerbFromRawDelta(
            CharacteristicModificationKind.Plain, delta: -3);

        verb.Should().Be(EffectVerb.Worsen);
        amount.Should().Be(3);
    }

    // RollThreshold/ArmourPenetration invert relative to Plain - raising the stored number is worse
    // for both families, so a positive raw delta resolves to Worsen, not Improve.
    [Theory]
    [InlineData(CharacteristicModificationKind.RollThreshold)]
    [InlineData(CharacteristicModificationKind.ArmourPenetration)]
    public void ResolveVerbFromRawDelta_InvertedFamilies_PositiveDeltaIsWorsen(CharacteristicModificationKind kind)
    {
        var (verb, amount) = CharacteristicModificationResolver.ResolveVerbFromRawDelta(kind, delta: 1);

        verb.Should().Be(EffectVerb.Worsen);
        amount.Should().Be(1);
    }

    [Theory]
    [InlineData(CharacteristicModificationKind.RollThreshold)]
    [InlineData(CharacteristicModificationKind.ArmourPenetration)]
    public void ResolveVerbFromRawDelta_InvertedFamilies_NegativeDeltaIsImprove(CharacteristicModificationKind kind)
    {
        var (verb, amount) = CharacteristicModificationResolver.ResolveVerbFromRawDelta(kind, delta: -1);

        verb.Should().Be(EffectVerb.Improve);
        amount.Should().Be(1);
    }

    [Fact]
    public void Resolve_ImproveOnAPlainCharacteristic_AddsTheAmount()
    {
        var resolved = CharacteristicModificationResolver.Resolve("S", new NumericCharacteristicValue(4),
            EffectVerb.Improve, 1);

        resolved.Should().Be(new NumericCharacteristicValue(5));
    }

    [Fact]
    public void Resolve_SetVerb_AssignsTheStatedValueDirectlyWithNoSignResolution()
    {
        var resolved = CharacteristicModificationResolver.Resolve("Oc", new NumericCharacteristicValue(2),
            EffectVerb.Set, 3);

        resolved.Should().Be(new NumericCharacteristicValue(3));
    }

    [Fact]
    public void Resolve_ClampsTheResolvedValueToItsCharacteristicsBound()
    {
        var resolved = CharacteristicModificationResolver.Resolve("AP", new NumericCharacteristicValue(0),
            EffectVerb.Worsen, 1);

        resolved.Should().Be(new NumericCharacteristicValue(0));
    }

    [Theory]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("N/A")]
    public void Resolve_ASymbolicCurrentValue_IsReturnedUnchanged(string symbol)
    {
        var current = new SymbolicCharacteristicValue(symbol);

        var resolved = CharacteristicModificationResolver.Resolve("Sv", current, EffectVerb.Improve, 1);

        resolved.Should().Be(current);
    }

    // Damage (dice-shaped) coverage - classify-weapon-characteristic-effects.

    [Fact]
    public void Clamp_DamageIsClassifiedAsPlain()
    {
        CharacteristicModificationKinds.Of("D").Should().Be(CharacteristicModificationKind.Plain);
    }

    [Fact]
    public void Resolve_ImprovingADiceShapedDamage_AddsToItsFlatModifier()
    {
        var resolved = CharacteristicModificationResolver.Resolve("D", new DiceCharacteristicValue(DiceExpression.D6),
            EffectVerb.Improve, 1);

        resolved.Should().Be(new DiceCharacteristicValue(DiceExpression.D6 + 1));
    }

    [Fact]
    public void Resolve_WorseningADiceShapedDamage_SubtractsFromItsFlatModifier()
    {
        var resolved = CharacteristicModificationResolver.Resolve("D",
            new DiceCharacteristicValue(DiceExpression.D6 + 2), EffectVerb.Worsen, 1);

        resolved.Should().Be(new DiceCharacteristicValue(DiceExpression.D6 + 1));
    }

    [Fact]
    public void Resolve_ImprovingAFixedDamage_BehavesIdenticallyToAPlainScalar()
    {
        var resolved = CharacteristicModificationResolver.Resolve("D",
            new DiceCharacteristicValue(DiceExpression.Fixed(2)),
            EffectVerb.Improve, 1);

        resolved.Should().Be(new DiceCharacteristicValue(DiceExpression.Fixed(3)));
    }

    [Fact]
    public void Resolve_SettingADiceShapedDamage_ReplacesItWithAFixedValue()
    {
        var resolved = CharacteristicModificationResolver.Resolve("D", new DiceCharacteristicValue(DiceExpression.D6),
            EffectVerb.Set, 3);

        resolved.Should().Be(new DiceCharacteristicValue(DiceExpression.Fixed(3)));
    }

    [Fact]
    public void Resolve_WorseningADiceShapedDamagePastItsFloor_ClampsItsModifierToAGuaranteedMinimumOf1()
    {
        // Unclamped, "D6" worsened by 8 would resolve to "D6-8" (guaranteed minimum
        // Count + Modifier = 1 + -8 = -7, per spec.md's own "flat modifier, plus one for each die
        // it rolls" definition). Clamped, the modifier is raised just enough that the guaranteed
        // minimum comes back to exactly 1: Modifier = 1 - Count = 1 - 1 = 0, i.e. plain "D6" - see
        // tasks.md task 4.4's note on design.md's own worked example (which states -5, an
        // arithmetic slip design.md has since been corrected to match this).
        var resolved = CharacteristicModificationResolver.Resolve("D", new DiceCharacteristicValue(DiceExpression.D6),
            EffectVerb.Worsen, 8);

        resolved.Should().Be(new DiceCharacteristicValue(DiceExpression.D6));
    }

    [Fact]
    public void Resolve_WorseningATwoDiceShapedDamagePastItsFloor_ClampsToANegativeModifier()
    {
        // A genuine negative-modifier clamp result (unlike the single-die case above, where
        // 1 - Count happens to be 0): 2D6 (guaranteed minimum 2) worsened by 5 would unclamp to
        // "2D6-5" (guaranteed minimum 2 + -5 = -3); clamped, Modifier = 1 - Count = 1 - 2 = -1,
        // i.e. "2D6-1" (guaranteed minimum 1).
        var twoD6 = DiceExpression.D6 with { Count = 2 };

        var resolved = CharacteristicModificationResolver.Resolve("D", new DiceCharacteristicValue(twoD6),
            EffectVerb.Worsen, 5);

        resolved.Should().Be(new DiceCharacteristicValue(twoD6 with { Modifier = -1 }));
    }

    [Fact]
    public void Resolve_WorseningADiceShapedDamageWithinItsFloor_LeavesItUnclamped()
    {
        // spec.md's own scenario: "D6" (guaranteed minimum 1) worsened by 3 clamps its flat
        // modifier so the guaranteed minimum stays 1, rather than resolving to "D6-3" (a
        // guaranteed minimum of -2) - the boundary case where the clamped-back modifier happens to
        // land exactly on 0 ("D6" itself), distinct from the amount-8 case above where it lands on
        // a real negative modifier.
        var resolved = CharacteristicModificationResolver.Resolve("D", new DiceCharacteristicValue(DiceExpression.D6),
            EffectVerb.Worsen, 3);

        resolved.Should().Be(new DiceCharacteristicValue(DiceExpression.D6));
    }

    [Fact]
    public void Resolve_WorseningAFixedDamagePastItsFloor_ClampsTo1()
    {
        var resolved = CharacteristicModificationResolver.Resolve("D",
            new DiceCharacteristicValue(DiceExpression.Fixed(2)),
            EffectVerb.Worsen, 5);

        resolved.Should().Be(new DiceCharacteristicValue(DiceExpression.Fixed(1)));
    }

    [Theory]
    [InlineData("S", -1, 1)] // improving S (Plain) past its floor of 1" clamps to 1" - the first
    // real weapon-characteristic proving example for S (task 4.5)
    [InlineData("AP", 1, 0)] // worsening a weapon's own AP past its cap of 0 clamps to 0 - the
    // first real weapon-characteristic proving example for AP (task 4.5)
    public void Clamp_WeaponCharacteristicsProveTheirExistingBounds(string characteristic, int value, int expected)
    {
        // S and AP were already in CharacteristicModificationKinds/CharacteristicModificationClamp's
        // lookup tables before this change (proven only against Statline values so far) - this is
        // their first real proving example against a genuine WeaponProfile-targeting Effect
        // (Chance for Glory/the ranged-Improve test above both classify real S/AP weapon Effects).
        // Design.md's Goals also names WS/BS, but this change's own weapon-characteristic vocabulary
        // (task 3.1) deliberately excludes Weapon Skill/Ballistic Skill - see tasks.md task 1.2's
        // finding - so they stay unproven by a real weapon Effect until a future phase widens that
        // vocabulary.
        CharacteristicModificationClamp.Apply(characteristic, value).Should().Be(expected);
    }

    [Fact]
    public void Resolve_ReproducesVexillaStatlineFlagRulesResolvedObjectiveControl()
    {
        // Ground-truth: Vexilla's own known-correct mutation is a plain +1 to Oc.
        var baseStatline = new Statline(6, 6, 2, 4, 7, 2); // Custodian Guard, base Oc 2

        var expected = new NumericCharacteristicValue(((NumericCharacteristicValue)baseStatline.Oc.Value).Value + 1);
        var viaKindResolver = CharacteristicModificationResolver.Resolve(
            "Oc", baseStatline.Oc.Value, EffectVerb.Improve, 1);

        viaKindResolver.Should().Be(expected);
    }
}