namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Resolves a classified <see cref="InvulnerableSaveCharacteristicEffect"/> plus its source
/// <see cref="Ability"/> into a real, displayable <see cref="InvulnerableSaveCharacteristicView"/> -
/// the InSv-specific counterpart to <see cref="CharacteristicModificationResolver"/>'s own scalar
/// resolution. <c>ProbHammer.Core.Domain.Roster.AttachedUnitAggregator</c> is this resolver's real
/// runtime consumer, matched against each present ability's normalized Text in the checked-in
/// <see cref="RuleClassificationBaseline"/>.</summary>
public static class InvulnerableSaveEffectResolver
{
    /// <summary>Produces a resolved (non-caveated) view whose derived value is
    /// <paramref name="effect"/>'s own melee/ranged pair, whose pre-mutation original value is
    /// preserved from <paramref name="current"/>'s own <c>OriginalValue</c> - never its effective
    /// <c>Value</c>, which may already reflect an earlier mutation - and whose contributing
    /// abilities record <paramref name="sourceAbility"/>.</summary>
    public static InvulnerableSaveCharacteristicView Resolve(
        InvulnerableSaveCharacteristicEffect effect, Ability sourceAbility,
        InvulnerableSaveCharacteristicView current) =>
        InvulnerableSaveCharacteristicView.Resolved(current.OriginalValue, effect.Value, [sourceAbility]);
}