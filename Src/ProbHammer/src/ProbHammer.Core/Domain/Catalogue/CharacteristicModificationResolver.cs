namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Resolves an <see cref="EffectVerb"/> plus a stated amount into a signed delta for a
/// characteristic's own <see cref="CharacteristicModificationKind"/>, and the top-level entry point
/// that applies that resolution (plus clamping) to a characteristic's actual
/// <see cref="CharacteristicValue"/> - see introduce-characteristic-modification-kind/design.md's
/// "Two separate small functions" and "Symbolic-value no-op is the caller-facing entry point's job"
/// decisions.</summary>
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

    /// <summary>Applies an effect to a characteristic's current <see cref="CharacteristicValue"/>,
    /// returning the resolved value. A symbolic current value ("-", "*", "N/A") is returned unchanged
    /// regardless of verb/characteristic/amount - see
    /// characteristic-modification-kind's "Symbolic Characteristic Values Are Never Modified"
    /// requirement. Not yet consumed by any caller - see this change's own Non-Goals.</summary>
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
