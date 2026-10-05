using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>Whether a classified effect applies on a unit: <see cref="Applied"/> (unconditional or
/// player-activated), <see cref="Suppressed"/> (an unselected option of a choice group with a
/// selection - neither applied nor shown), or <see cref="NotApplied"/> (shown as not added).</summary>
public enum EffectState
{
    Applied,
    Suppressed,
    NotApplied
}

public static class EffectStates
{
    public static EffectState Of(AbilityClassification classification, ClassifiedEffect effect,
        AbilityActivation? activation)
    {
        if (classification.IsUnconditional(effect))
            return EffectState.Applied;

        if (effect.ChoiceBranch is { } branch)
        {
            if (activation is null || !activation.Choices.TryGetValue(branch.Group, out var selected) ||
                selected.Count == 0)
                return EffectState.NotApplied;

            return selected.Contains(branch.Option) ? EffectState.Applied : EffectState.Suppressed;
        }

        return activation is not null && activation.Conditions.Contains(ConditionKey(effect))
            ? EffectState.Applied
            : EffectState.NotApplied;
    }

    /// <summary>The activation key of a non-choice conditional effect: its condition text, or the empty
    /// string when only a usage limit or turn restriction makes it conditional.</summary>
    public static string ConditionKey(ClassifiedEffect effect) =>
        string.IsNullOrWhiteSpace(effect.ConditionText) ? "" : effect.ConditionText.Trim();
}