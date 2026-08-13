using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>
/// Builds the Attached Unit aggregate view: every distinct statline, weapon, and ability still in
/// play across a Unit's or AttachedUnit's present model-lines, recomputed live from casualty
/// state rather than cached.
/// </summary>
public static class AttachedUnitAggregator
{
    public static AttachedUnitAggregateView Build(ICombatUnit combatUnit)
    {
        var presentLines = combatUnit.Components
            .SelectMany(unit => unit.ModelLines.Select(modelLine => (Unit: unit, ModelLine: modelLine)))
            .Where(x => x.ModelLine.RemainingCount > 0)
            .ToList();

        return new AttachedUnitAggregateView(
            Name: combatUnit.Name,
            IsAttachedUnit: combatUnit is AttachedUnit,
            Statlines: BuildStatlines(combatUnit),
            Weapons: BuildWeapons(presentLines),
            Abilities: BuildAbilities(combatUnit),
            Keywords: KeywordResolution.EffectiveKeywords(combatUnit));
    }

    // Component display order: an AttachedUnit's Attached units first, in their list order, then
    // the Bodyguard; a plain Unit is just itself. Shared by BuildStatlines and BuildAbilities so
    // both walk components in the same order.
    private static IReadOnlyList<Unit> ComponentDisplayOrder(ICombatUnit combatUnit) =>
        combatUnit is AttachedUnit attachedUnit
            ? [.. attachedUnit.Attached, attachedUnit.Bodyguard]
            : combatUnit.Components;

    // Walks components in display order, and within each component, its Datasheet's declared
    // Statlines in order. Matching each declared name against only that component's own
    // ModelLines (never another component's) is what makes per-component merge scoping fall out
    // for free - two components can never combine into one entry, and there's no separate sort
    // step to disagree with the grouping.
    private static IReadOnlyList<AggregateStatlineEntry> BuildStatlines(ICombatUnit combatUnit)
    {
        var components = ComponentDisplayOrder(combatUnit);

        var entries = new List<AggregateStatlineEntry>();
        foreach (var component in components)
        {
            foreach (var (statlineName, statline) in component.Datasheet.Statlines)
            {
                var lines = component.ModelLines
                    .Where(ml => string.Equals(ml.StatlineName, statlineName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!lines.Any(ml => ml.RemainingCount > 0))
                    continue;

                var loadouts = lines
                    .Select(ml => new ModelLineLoadout(
                        WeaponsLabel: string.Join(", ", ml.Weapons),
                        RemainingCount: ml.RemainingCount,
                        InitialCount: ml.Count))
                    .ToList();

                entries.Add(new AggregateStatlineEntry(
                    ComponentName: component.Datasheet.Name,
                    StatlineName: statlineName,
                    Statline: statline,
                    RemainingCount: lines.Sum(ml => ml.RemainingCount),
                    InitialCount: lines.Sum(ml => ml.Count),
                    Loadouts: loadouts));
            }
        }

        return entries;
    }

    // Groups weapons by structural profile equality (WeaponProfile.EqualityKey), aggregating a true
    // TotalAttacks (per contributing model-line: PerModelAttacks.Scale(RemainingCount), Add-reduced
    // across every contributor sharing the key) and retaining per-contribution provenance - the
    // aggregation concept ported from SimulationAdapter's weapon-group-by-equality-key algorithm.
    private static IReadOnlyList<AggregateWeaponEntry> BuildWeapons(
        List<(Unit Unit, ModelLine ModelLine)> presentLines)
    {
        var groups = new Dictionary<WeaponProfileEqualityKey,
            (WeaponProfile Profile, DiceExpression TotalAttacks, List<WeaponContribution> Contributions)>();

        foreach (var (unit, modelLine) in presentLines)
        {
            foreach (var weaponName in modelLine.Weapons)
            {
                var profile = unit.Datasheet.ResolveWeaponProfile(weaponName);
                var key = profile.EqualityKey();

                var contribution = new WeaponContribution(
                    ComponentName: unit.Datasheet.Name,
                    StatlineName: modelLine.StatlineName,
                    Count: modelLine.RemainingCount,
                    PerModelAttacks: profile.A);
                var scaledAttacks = profile.A.Scale(modelLine.RemainingCount);

                if (groups.TryGetValue(key, out var existing))
                {
                    existing.Contributions.Add(contribution);
                    groups[key] = (existing.Profile, existing.TotalAttacks.Add(scaledAttacks), existing.Contributions);
                }
                else
                {
                    groups[key] = (profile, scaledAttacks, [contribution]);
                }
            }
        }

        return groups.Values
            .Select(v => new AggregateWeaponEntry(v.Profile, v.TotalAttacks, v.Contributions))
            .ToList();
    }

    // Walks components in display order; for each present component, reports its Datasheet's own
    // Abilities (component-wide, StatlineName: null) and each of its present ModelLines' own
    // Abilities (StatlineName: that line's own name) - regardless of Scope, and with no
    // cross-component combination or deduplication. The explicit IsPresent guard is needed only
    // for Datasheet-sourced entries: unlike BuildStatlines, where a fully-dead component naturally
    // produces zero entries through its per-statline RemainingCount check, a Datasheet ability has
    // no statline-level gate of its own to fall through.
    private static IReadOnlyList<AggregateAbilityEntry> BuildAbilities(ICombatUnit combatUnit)
    {
        var entries = new List<AggregateAbilityEntry>();

        foreach (var component in ComponentDisplayOrder(combatUnit))
        {
            if (!component.IsPresent)
                continue;

            foreach (var ability in component.Datasheet.Abilities)
                entries.Add(new AggregateAbilityEntry(component.Datasheet.Name, StatlineName: null, ability));

            foreach (var modelLine in component.ModelLines.Where(ml => ml.RemainingCount > 0))
            foreach (var ability in modelLine.Abilities)
                entries.Add(new AggregateAbilityEntry(component.Datasheet.Name, modelLine.StatlineName, ability));
        }

        return entries;
    }
}
