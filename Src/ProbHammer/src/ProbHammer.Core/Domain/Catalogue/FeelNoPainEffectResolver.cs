namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Combines a granted Feel No Pain threshold with a <see cref="Statline.Fnp"/> value. Unlike
/// <see cref="InvulnerableSaveEffectResolver.Merge"/>'s caller, it compares thresholds even when the
/// current value already has a source, since a bearer's own FNP always does.</summary>
public static class FeelNoPainEffectResolver
{
    /// <summary>Keeps the better threshold. An improving grant replaces the current source; otherwise
    /// <paramref name="current"/> is returned unchanged.</summary>
    public static ScalarCharacteristicView Merge(int threshold, Ability sourceAbility, ScalarCharacteristicView current) =>
        Threshold(current) is { } existing && existing <= threshold
            ? current
            : ScalarCharacteristicView.Resolved(current.OriginalValue, threshold, [sourceAbility]);

    public static ScalarCharacteristicView Merge(FeelNoPainEffect effect, Ability sourceAbility,
        ScalarCharacteristicView current) =>
        Merge(effect.Value, sourceAbility, current);

    /// <summary>The value's threshold, or null when it has no Feel No Pain.</summary>
    public static int? Threshold(ScalarCharacteristicView fnp) =>
        fnp.Value is NumericCharacteristicValue numeric ? numeric.Value : null;
}
