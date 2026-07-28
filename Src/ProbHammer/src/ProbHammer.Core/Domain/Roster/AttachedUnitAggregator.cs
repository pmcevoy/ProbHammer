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
            Statlines: BuildStatlines(presentLines),
            Weapons: BuildWeapons(presentLines),
            UnitScopedAbilities: BuildUnitScopedAbilities(combatUnit, presentLines),
            ModelScopedAbilities: BuildModelScopedAbilities(presentLines),
            Keywords: KeywordResolution.EffectiveKeywords(combatUnit));
    }

    private static IReadOnlyList<AggregateStatlineEntry> BuildStatlines(
        List<(Unit Unit, ModelLine ModelLine)> presentLines) =>
        presentLines
            .Select(x => x.ModelLine.StatlineName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name =>
            {
                var owner = presentLines.First(x =>
                    string.Equals(x.ModelLine.StatlineName, name, StringComparison.OrdinalIgnoreCase));
                return new AggregateStatlineEntry(name, owner.Unit.Datasheet.GetStatline(name));
            })
            .ToList();

    // Groups weapons by structural profile equality (WeaponProfile.EqualityKey), summing the
    // remaining counts of all model-lines carrying that profile - the aggregation concept ported
    // from SimulationAdapter's weapon-group-by-equality-key algorithm.
    private static IReadOnlyList<AggregateWeaponEntry> BuildWeapons(
        List<(Unit Unit, ModelLine ModelLine)> presentLines)
    {
        var groups = new Dictionary<WeaponProfileEqualityKey, (WeaponProfile Profile, int Count)>();

        foreach (var (unit, modelLine) in presentLines)
        {
            foreach (var weaponName in modelLine.Weapons)
            {
                var profile = unit.Datasheet.ResolveWeaponProfile(weaponName);
                var key = profile.EqualityKey();

                groups[key] = groups.TryGetValue(key, out var existing)
                    ? (existing.Profile, existing.Count + modelLine.RemainingCount)
                    : (profile, modelLine.RemainingCount);
            }
        }

        return groups.Values.Select(v => new AggregateWeaponEntry(v.Profile, v.Count)).ToList();
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
