namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Resolves a classified <see cref="InvulnerableSaveCharacteristicEffect"/> plus its source
/// <see cref="Ability"/> into a real, displayable <see cref="InvulnerableSaveCharacteristicView"/> -
/// the InSv-specific counterpart to <see cref="CharacteristicModificationResolver"/>'s own scalar
/// resolution. <c>ProbHammer.Core.Domain.Roster.AttachedUnitAggregator</c> is this resolver's real
/// runtime consumer, matched against each present ability.s classification in the
/// <see cref="AbilityClassificationCatalogue"/>.</summary>
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

    /// <summary>Resolves a footnoted InSv from its linked ability's effect. A side the effect leaves
    /// at <c>0</c> keeps the split form's plain value ("4+* / 5+"), but not a bare footnote's
    /// ("4+*"), where the stored value on that side is the footnoted digit itself.</summary>
    public static InvulnerableSaveCharacteristicView ResolveCaveat(
        InvulnerableSaveCharacteristicEffect effect, Ability sourceAbility,
        InvulnerableSaveCharacteristicView current)
    {
        var footnoted = Math.Max(effect.Value.MeleeInSv, effect.Value.RangedInSv);
        var resolved = new InvulnerableSave(
            Side(effect.Value.MeleeInSv, current.Value.MeleeInSv),
            Side(effect.Value.RangedInSv, current.Value.RangedInSv));
        return InvulnerableSaveCharacteristicView.Resolved(current.OriginalValue, resolved, [sourceAbility]);

        int Side(int granted, int stored) => granted != 0 ? granted : stored == footnoted ? 0 : stored;
    }

    /// <summary>Combines a granted save with <paramref name="current"/>'s value, keeping the better
    /// save per side, so a one-sided or weaker grant never removes or worsens an existing save.
    /// Returns <paramref name="current"/> unchanged when the grant improves neither side.</summary>
    public static InvulnerableSaveCharacteristicView Merge(
        InvulnerableSaveCharacteristicEffect effect, Ability sourceAbility,
        InvulnerableSaveCharacteristicView current)
    {
        var merged = new InvulnerableSave(
            Better(current.Value.MeleeInSv, effect.Value.MeleeInSv),
            Better(current.Value.RangedInSv, effect.Value.RangedInSv));

        return merged == current.Value
            ? current
            : InvulnerableSaveCharacteristicView.Resolved(
                current.OriginalValue, merged, [.. current.ContributingAbilities, sourceAbility]);
    }

    // 0 is "no save"; otherwise a lower roll is the better save.
    private static int Better(int existing, int granted) =>
        existing == 0 ? granted : granted == 0 ? existing : Math.Min(existing, granted);
}