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

    /// <summary>Applies an effect to a characteristic's current <see cref="CharacteristicValue"/>,
    /// returning the resolved value. A symbolic current value ("-", "*", "N/A") is returned unchanged
    /// regardless of verb/characteristic/amount. A <see cref="DiceCharacteristicValue"/> (Damage, the
    /// first dice-shaped characteristic this resolves) takes its own branch, per
    /// `classify-weapon-characteristic-effects` design.md's "Damage resolves through the Plain
    /// family" decision: <see cref="EffectVerb.Improve"/>/<see cref="EffectVerb.Worsen"/> apply the
    /// resolved signed delta to the value's own flat modifier via <see cref="DiceExpression"/>'s
    /// existing <c>+</c> operator, preserving its dice component (count/sides) unchanged; a
    /// <see cref="EffectVerb.Set"/> replaces the value outright with a fixed
    /// <see cref="DiceExpression"/>, discarding any prior dice component - consistent with how Set
    /// already discards a prior value's history for every other characteristic. Not yet consumed by
    /// any caller.</summary>
    public static CharacteristicValue Resolve(
        string characteristic, CharacteristicValue current, EffectVerb verb, int amount)
    {
        if (current is DiceCharacteristicValue dice)
        {
            if (verb == EffectVerb.Set)
                return new DiceCharacteristicValue(
                    CharacteristicModificationClamp.ApplyToDice(characteristic, DiceExpression.Fixed(amount)));

            var delta = ResolveDelta(CharacteristicModificationKinds.Of(characteristic), verb, amount);
            var resolvedDice = dice.Value + delta;
            return new DiceCharacteristicValue(
                CharacteristicModificationClamp.ApplyToDice(characteristic, resolvedDice));
        }

        if (current is not NumericCharacteristicValue numeric)
            return current;

        var resolved = verb == EffectVerb.Set
            ? amount
            : numeric.Value + ResolveDelta(CharacteristicModificationKinds.Of(characteristic), verb, amount);

        return new NumericCharacteristicValue(CharacteristicModificationClamp.Apply(characteristic, resolved));
    }

    /// <summary>Resolves a classified <see cref="WeaponCharacteristicEffect"/> naming the Attacks
    /// characteristic into its signed per-model amount - the weapon-Attacks counterpart to
    /// <see cref="Resolve"/>'s own mutated-profile path, which never covers Attacks (see
    /// <see cref="WeaponCharacteristicEffectResolver"/>'s own doc comment). Throws for a Set verb,
    /// matching <see cref="ResolveDelta"/>'s own existing behavior - no real corpus example uses
    /// one. Callers are responsible for passing only an effect whose Characteristic is "A".</summary>
    public static int ResolveAttacksAmount(WeaponCharacteristicEffect effect) =>
        ResolveDelta(CharacteristicModificationKinds.Of("A"), effect.Verb, effect.Amount);
}