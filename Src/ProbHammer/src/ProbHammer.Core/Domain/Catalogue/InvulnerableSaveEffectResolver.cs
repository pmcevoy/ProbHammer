namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Resolves a classified <see cref="InvulnerableSaveCharacteristicEffect"/> plus its source
/// <see cref="Ability"/> into a real, displayable <see cref="InvulnerableSaveCharacteristicView"/> -
/// the InSv-specific counterpart to <see cref="CharacteristicModificationResolver"/>'s own scalar
/// resolution, proven correct in isolation ahead of any roster-resolution-time wiring. See
/// resolve-invulnerable-save-effects/design.md's Non-Goals and Migration Plan step 4 - nothing in
/// this change wires this into <c>AttachedUnitAggregator</c>/<c>StatlineFlagRuleCatalogue</c>;
/// <c>ShieldDomeStatlineFlagRule</c> remains the only thing that actually produces a real
/// <see cref="InvulnerableSaveCharacteristicView"/> today.</summary>
public static class InvulnerableSaveEffectResolver
{
    /// <summary>Produces a resolved (non-caveated) view whose derived value is
    /// <paramref name="effect"/>'s own melee/ranged pair, whose pre-mutation original value is
    /// preserved from <paramref name="current"/>'s own <c>OriginalValue</c> - never its effective
    /// <c>Value</c>, which may already reflect an earlier mutation - and whose contributing
    /// abilities record <paramref name="sourceAbility"/>. Mirrors every other hand-authored
    /// <c>Resolved(...)</c> call site in this codebase (e.g. <c>ShieldDomeStatlineFlagRule.Apply</c>)
    /// that mutates an existing characteristic on top of its own true catalogue base.</summary>
    public static InvulnerableSaveCharacteristicView Resolve(
        InvulnerableSaveCharacteristicEffect effect, Ability sourceAbility,
        InvulnerableSaveCharacteristicView current) =>
        InvulnerableSaveCharacteristicView.Resolved(current.OriginalValue, effect.Value, [sourceAbility]);
}
