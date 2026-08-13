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
            UnitScopedAbilities: BuildUnitScopedAbilities(combatUnit, presentLines),
            ModelScopedAbilities: BuildModelScopedAbilities(presentLines),
            Keywords: KeywordResolution.EffectiveKeywords(combatUnit));
    }

    // Walks components in display order (an AttachedUnit's Attached units first, in their list
    // order, then the Bodyguard; a plain Unit is just itself), and within each component, its
    // Datasheet's declared Statlines in order. Matching each declared name against only that
    // component's own ModelLines (never another component's) is what makes per-component merge
    // scoping fall out for free - two components can never combine into one entry, and there's no
    // separate sort step to disagree with the grouping.
    private static IReadOnlyList<AggregateStatlineEntry> BuildStatlines(ICombatUnit combatUnit)
    {
        IReadOnlyList<Unit> components = combatUnit is AttachedUnit attachedUnit
            ? [.. attachedUnit.Attached, attachedUnit.Bodyguard]
            : combatUnit.Components;

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

    private static IReadOnlyList<Ability> BuildUnitScopedAbilities(
        ICombatUnit combatUnit, List<(Unit Unit, ModelLine ModelLine)> presentLines)
    {
        var presentUnits = combatUnit.Components.Where(u => u.IsPresent);

        var fromDatasheets = presentUnits.SelectMany(u => u.Datasheet.Abilities);
        var fromModelLines = presentLines.SelectMany(x => x.ModelLine.Abilities);

        return fromDatasheets.Concat(fromModelLines)
            .Where(a => a.Scope == AbilityScope.Unit)
            .Distinct()
            .ToList();
    }

    private static IReadOnlyList<ModelScopedAbilityEntry> BuildModelScopedAbilities(
        List<(Unit Unit, ModelLine ModelLine)> presentLines) =>
        presentLines
            .SelectMany(x => x.ModelLine.Abilities
                .Where(a => a.Scope == AbilityScope.Model)
                .Select(a => new ModelScopedAbilityEntry(x.ModelLine, a)))
            .ToList();
}
