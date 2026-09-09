using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

/// <summary>Covers introduce-characteristic-modification-kind's requirements: sign resolution per
/// arithmetic family, per-characteristic clamp bounds, symbolic-value no-op, and the proving
/// requirement against Vexilla's own known-correct Objective Control mutation (see that change's
/// design.md for why Shield Dome/InSv is not a valid proving example here).</summary>
public class CharacteristicModificationResolverTests
{
    // Rulebook worked examples (.claude/vnext-ideas.md, quoted verbatim from the user):
    // "WS 3+ improved by 1 -> 2+"; "WS 3+ worsened by 1 -> 4+"; "AP -1 improved by 1 -> -2";
    // "AP -1 worsened by 1 -> 0"; "S improved by 1 -> +1" (a Plain characteristic).
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

    // widen-baseline-generation-coverage's own inverse function: raw stored-value delta -> rulebook
    // verb, the mirror image of ResolveDelta above. Auric Mantle's real corpus modifier is a
    // structural "increment" of 2 on Toughness's own Plain-family characteristic (W, in this
    // Datasheet's own field allowlist) - a positive raw delta on a Plain characteristic is Improve,
    // sign carried straight through.
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

    [Fact]
    public void Resolve_ReproducesVexillaStatlineFlagRulesResolvedObjectiveControl()
    {
        // Ground-truth: Vexilla's own known-correct mutation is a plain +1 to Oc (the now-retired
        // VexillaStatlineFlagRule.Apply's own "current + 1" - apply-rule-effect-baseline replaced it
        // with this general resolver, run from the checked-in baseline).
        var baseStatline = new Statline(6, 6, 2, 4, 7, 2); // Custodian Guard, base Oc 2

        var expected = new NumericCharacteristicValue(((NumericCharacteristicValue)baseStatline.Oc.Value).Value + 1);
        var viaKindResolver = CharacteristicModificationResolver.Resolve(
            "Oc", baseStatline.Oc.Value, EffectVerb.Improve, 1);

        viaKindResolver.Should().Be(expected);
    }
}