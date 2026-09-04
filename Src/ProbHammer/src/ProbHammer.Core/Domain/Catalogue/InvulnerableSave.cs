namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>A Statline's invulnerable save, split by attack type. Carries no caveat concept of its
/// own - whether a save is caveated, and which ability (if any) is responsible for it not simply
/// being the Datasheet's own base value, are properties of the wrapping
/// <see cref="InvulnerableSaveCharacteristicView"/>, not this value (see
/// unify-invulnerable-save-characteristic-view).</summary>
public sealed record InvulnerableSave(int MeleeInSv, int RangedInSv)
{
    /// <summary>No invulnerable save at all - mirrors DiceExpression.D3/D6's own static-preset
    /// convention for a specific, named, frequently-constructed value rather than an arbitrary one
    /// (added post-implementation after `new(0, 0)` was found recurring at several real call sites -
    /// unify-invulnerable-save-characteristic-view).</summary>
    public static readonly InvulnerableSave None = new(0, 0);

    /// <summary>Uniform saves (melee == ranged) are the common case - mirrors DiceExpression's
    /// implicit int conversion so a plain digit reads naturally at call sites.</summary>
    public static implicit operator InvulnerableSave(int uniformValue) => new(uniformValue, uniformValue);
}