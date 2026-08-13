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
        var allLines = combatUnit.Components
            .SelectMany(unit => unit.ModelLines.Select(modelLine => (Unit: unit, ModelLine: modelLine)))
            .ToList();

        var presentLines = allLines.Where(x => x.ModelLine.RemainingCount > 0).ToList();

        return new AttachedUnitAggregateView(
            Name: combatUnit.Name,
            Statlines: BuildStatlines(allLines),
            Weapons: BuildWeapons(presentLines),
            UnitScopedAbilities: BuildUnitScopedAbilities(combatUnit, presentLines),
            ModelScopedAbilities: BuildModelScopedAbilities(presentLines),
            Keywords: KeywordResolution.EffectiveKeywords(combatUnit));
    }

    // Groups by StatlineName across every model-line belonging to a component, regardless of that
    // line's own RemainingCount, so a fully-wiped loadout-variant still contributes its InitialCount
    // and still appears in the Loadouts breakdown (as 0 remaining) as long as another model-line
    // sharing the same statline name still has models remaining.
    private static IReadOnlyList<AggregateStatlineEntry> BuildStatlines(
        List<(Unit Unit, ModelLine ModelLine)> allLines) =>
        allLines
            .GroupBy(x => x.ModelLine.StatlineName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Any(x => x.ModelLine.RemainingCount > 0))
            .Select(group =>
            {
                var owner = group.First().Unit;
                var loadouts = group
                    .Select(x => new ModelLineLoadout(
                        WeaponsLabel: string.Join(", ", x.ModelLine.Weapons),
                        RemainingCount: x.ModelLine.RemainingCount,
                        InitialCount: x.ModelLine.Count))
                    .ToList();

                return new AggregateStatlineEntry(
                    StatlineName: group.Key,
                    Statline: owner.Datasheet.GetStatline(group.Key),
                    RemainingCount: group.Sum(x => x.ModelLine.RemainingCount),
                    InitialCount: group.Sum(x => x.ModelLine.Count),
                    Loadouts: loadouts);
            })
            .ToList();

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
