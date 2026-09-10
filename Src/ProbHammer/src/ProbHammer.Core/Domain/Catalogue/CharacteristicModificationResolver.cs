namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Resolves an <see cref="EffectVerb"/> plus a stated amount into a signed delta for a
/// characteristic's own <see cref="CharacteristicModificationKind"/>, and the top-level entry point
/// that applies that resolution (plus clamping) to a characteristic's actual
/// <see cref="CharacteristicValue"/>.</summary>
public static class CharacteristicModificationResolver
{
    /// <summary>Pure sign arithmetic, ignorant of any current value or clamp bound. <see
    /// cref="EffectVerb.Set"/> is deliberately not handled here - a Set assigns its stated value
    /// directly with no sign resolution, so callers skip this function entirely for that verb (see
    /// <see cref="Resolve"/>).</summary>
    public static int ResolveDelta(CharacteristicModificationKind kind, EffectVerb verb, int amount) =>
        (kind, verb) switch
        {
            (CharacteristicModificationKind.RollThreshold, EffectVerb.Improve) => -amount,
            (CharacteristicModificationKind.RollThreshold, EffectVerb.Worsen) => amount,
            (CharacteristicModificationKind.ArmourPenetration, EffectVerb.Improve) => -amount,
            (CharacteristicModificationKind.ArmourPenetration, EffectVerb.Worsen) => amount,
            (CharacteristicModificationKind.Plain, EffectVerb.Improve) => amount,
            (CharacteristicModificationKind.Plain, EffectVerb.Worsen) => -amount,
            (_, EffectVerb.Set) => throw new ArgumentException(
                "Set assigns its value directly - callers must not resolve a delta for it.", nameof(verb)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    /// <summary>The inverse of <see cref="ResolveDelta"/>: given a characteristic's own arithmetic
    /// family and an already-signed raw delta (e.g. BSData's own raw stored-value change - a positive
    /// delta for an "increment" modifier, negative for "decrement"), resolves the rulebook
    /// <see cref="EffectVerb"/> (<see cref="EffectVerb.Improve"/> or <see cref="EffectVerb.Worsen"/>
    /// only - never <see cref="EffectVerb.Set"/>, which has no delta/sign concept and is handled
    /// separately by callers) plus the
    /// unsigned amount that verb states. <see cref="CharacteristicModificationKind.Plain"/> carries
    /// the sign straight through (a positive raw delta is an Improve); RollThreshold/ArmourPenetration
    /// invert it (raising the stored number is worse for both families) - the mirror image of
    /// <see cref="ResolveDelta"/>'s own three-family switch, in the opposite direction. A zero delta
    /// has no real corpus meaning (every recognized raw modifier states a genuine change) and is
    /// treated as Improve 0 - Plain's own sign rule applied literally, never specially cased.</summary>
    public static (EffectVerb Verb, int Amount)
        ResolveVerbFromRawDelta(CharacteristicModificationKind kind, int delta) =>
        kind switch
        {
            CharacteristicModificationKind.Plain => delta < 0
                ? (EffectVerb.Worsen, -delta)
                : (EffectVerb.Improve, delta),
            CharacteristicModificationKind.RollThreshold or CharacteristicModificationKind.ArmourPenetration =>
                delta < 0 ? (EffectVerb.Improve, -delta) : (EffectVerb.Worsen, delta),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    /// <summary>Applies an effect to a characteristic's current <see cref="CharacteristicValue"/>,
    /// returning the resolved value. A symbolic current value ("-", "*", "N/A") is returned unchanged
    /// regardless of verb/characteristic/amount. Not yet consumed by any caller.</summary>
    public static CharacteristicValue Resolve(
        string characteristic, CharacteristicValue current, EffectVerb verb, int amount)
    {
        if (current is not NumericCharacteristicValue numeric)
            return current;

        var resolved = verb == EffectVerb.Set
            ? amount
            : numeric.Value + ResolveDelta(CharacteristicModificationKinds.Of(characteristic), verb, amount);

        return new NumericCharacteristicValue(CharacteristicModificationClamp.Apply(characteristic, resolved));
    }
}