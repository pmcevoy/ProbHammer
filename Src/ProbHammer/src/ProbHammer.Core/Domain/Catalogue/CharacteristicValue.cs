namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Abstract base with sealed <see cref="NumericCharacteristicValue"/>/
/// <see cref="DiceCharacteristicValue"/>/<see cref="SymbolicCharacteristicValue"/> subtypes rather
/// than one record with a Kind field, so a characteristic's raw value can never carry more than one
/// kind at once or none at all - mirrors <see cref="WeaponProfile"/>'s own abstract-base/
/// sealed-subtype shape. See introduce-characteristic-domain-model/design.md's Decision 1.</summary>
public abstract record CharacteristicValue
{
    /// <summary>Uniform with <see cref="DiceExpression"/>'s/<see cref="InvulnerableSave"/>'s own
    /// implicit int conversion, so a plain digit reads naturally at a call site expecting a
    /// CharacteristicValue.</summary>
    public static implicit operator CharacteristicValue(int value) => new NumericCharacteristicValue(value);

    /// <summary>Symmetric with the int conversion above - a plain DiceExpression (e.g.
    /// DiceExpression.D6 + 6) reads naturally at a call site expecting a CharacteristicValue, with
    /// no explicit DiceCharacteristicValue wrap needed.</summary>
    public static implicit operator CharacteristicValue(DiceExpression value) => new DiceCharacteristicValue(value);
}

public sealed record NumericCharacteristicValue(int Value) : CharacteristicValue
{
    public override string ToString() => Value.ToString();
}

public sealed record DiceCharacteristicValue(DiceExpression Value) : CharacteristicValue
{
    public override string ToString() => Value.ToString();
}

/// <summary>A non-numeric characteristic value, e.g. "-", "*", "N/A". Symbol is deliberately an
/// unconstrained string, not a closed enum/allowlist - real BSData source text is not guaranteed
/// to be limited to those three examples, and this layer has no requirement to validate or
/// normalize it (see the project's general convention against validating values that don't yet
/// have a proven failure mode).</summary>
public sealed record SymbolicCharacteristicValue(string Symbol) : CharacteristicValue
{
    public override string ToString() => Symbol;
}