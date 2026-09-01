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

        var abilities = BuildAbilities(combatUnit);

        return new AttachedUnitAggregateView(
            Name: combatUnit.Name,
            IsAttachedUnit: combatUnit is AttachedUnit,
            Statlines: ApplyStatlineFlagRules(BuildStatlines(combatUnit), abilities),
            Weapons: BuildWeapons(presentLines),
            Abilities: abilities,
            Keywords: KeywordResolution.EffectiveKeywords(combatUnit));
    }

    // Runs after BuildStatlines/BuildAbilities produce their live, casualty-filtered results (design
    // D3) - abilities is already filtered to only currently-present sources, so a matched rule's
    // liveness falls out for free with no separate tracking (statline-flag-rules' "Mutation Liveness
    // Follows Ability Presence"). Never mutates Datasheet/Unit; only the returned decorated copy of
    // the statline entries carries a rule's effect.
    private static IReadOnlyList<AggregateStatlineEntry> ApplyStatlineFlagRules(
        IReadOnlyList<AggregateStatlineEntry> statlines, IReadOnlyList<AggregateAbilityEntry> abilities)
    {
        var matches = abilities
            .Select(a => (Entry: a, Rule: StatlineFlagRuleCatalogue.All.FirstOrDefault(r => r.Matches(a.Ability))))
            .Where(x => x.Rule is not null)
            .ToList();

        if (matches.Count == 0)
            return statlines;

        return statlines.Select(entry =>
        {
            var applicable = matches.Where(m => IsBearer(m.Entry, m.Rule!, entry)).ToList();
            if (applicable.Count == 0)
                return entry;

            var mutated = entry.Statline;
            var flags = new List<StatlineFlag>(entry.Flags);
            foreach (var (abilityEntry, rule) in applicable)
            {
                mutated = rule!.Apply(mutated);
                flags.Add(new StatlineFlag(rule.Characteristic, abilityEntry.Ability));
            }

            return entry with { Statline = mutated, Flags = flags };
        }).ToList();
    }

    // A bearer-only rule's bearer is the matched ability's own (ComponentName, StatlineName) - one
    // specific model-line when StatlineName is set, the whole component when it's null (a
    // Datasheet-level or Enhancement-sourced ability, per D4). A whole-unit rule applies to every
    // row regardless, since the matched ability is already confirmed present on this ICombatUnit.
    private static bool IsBearer(AggregateAbilityEntry abilityEntry, StatlineFlagRule rule,
        AggregateStatlineEntry statlineEntry)
    {
        if (rule.Scope == StatlineFlagRuleScope.WholeUnit)
            return true;

        return abilityEntry.StatlineName is not null
            ? abilityEntry.ComponentName == statlineEntry.ComponentName &&
              abilityEntry.StatlineName == statlineEntry.StatlineName
            : abilityEntry.ComponentName == statlineEntry.ComponentName;
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

                if (lines.Count == 0)
                    continue;

                var loadouts = lines
                    .Select(ml => new ModelLineLoadout(
                        WeaponsLabel: string.Join(", ", ml.Weapons),
                        Weapons: ml.Weapons,
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
                    PerModelAttacks: profile.A,
                    LoadoutIndex: LoadoutIndexOf(unit, modelLine));
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

    // Mirrors the same (unfiltered by RemainingCount) statline-name match BuildStatlines uses to
    // build a statline entry's Loadouts list, so this index always lines up with that ModelLine's
    // position in Loadouts - including when a dead sibling loadout still occupies an earlier slot.
    // -1 when the statline has only one ModelLine (BuildStatlines renders no Loadouts breakdown at
    // all in that case, so there is nothing to index).
    private static int LoadoutIndexOf(Unit unit, ModelLine modelLine)
    {
        var lines = unit.ModelLines
            .Where(ml => string.Equals(ml.StatlineName, modelLine.StatlineName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return lines.Count <= 1 ? -1 : lines.IndexOf(modelLine);
    }

    // Walks components in display order; for each present component, reports its Datasheet's own
    // Abilities, its own resolved Enhancements (both component-wide, StatlineName: null - an
    // Enhancement's Ability.Origin is what a renderer reads to show it distinctly, not this
    // record's own shape), and each of its present ModelLines' own Abilities (StatlineName: that
    // line's own name) - regardless of Scope, and with no cross-component combination or
    // deduplication. The explicit IsPresent guard is needed for both component-wide sources
    // (Datasheet.Abilities and Enhancements alike): unlike BuildStatlines, where a fully-dead
    // component naturally produces zero entries through its per-statline RemainingCount check,
    // neither has a statline-level gate of its own to fall through.
    private static IReadOnlyList<AggregateAbilityEntry> BuildAbilities(ICombatUnit combatUnit)
    {
        var entries = new List<AggregateAbilityEntry>();

        foreach (var component in ComponentDisplayOrder(combatUnit))
        {
            if (!component.IsPresent)
                continue;

            foreach (var ability in component.Datasheet.Abilities)
                entries.Add(new AggregateAbilityEntry(component.Datasheet.Name, StatlineName: null, ability));

            foreach (var ability in component.Enhancements)
                entries.Add(new AggregateAbilityEntry(component.Datasheet.Name, StatlineName: null, ability));

            foreach (var modelLine in component.ModelLines.Where(ml => ml.RemainingCount > 0))
            foreach (var ability in modelLine.Abilities)
                entries.Add(new AggregateAbilityEntry(component.Datasheet.Name, modelLine.StatlineName, ability));
        }

        return PromoteArmyRuleAbilities(entries);
    }

    /// <summary>An ArmyRule-origin ability (see AbilityOrigin.ArmyRule - a Core rule whose own
    /// gating is chapter/sub-faction exclusive, e.g. "Templar Vows"/"Oath of Moment") is an
    /// army-wide fact, never a per-component one - so it ALWAYS gets promoted to belong to no
    /// single component (<see cref="AggregateAbilityEntry.ComponentName"/> null,
    /// <see cref="AggregateAbilityEntry.ContributingComponentNames"/> listing every contributor),
    /// regardless of how many present components in THIS roster happen to reference it - even a
    /// standalone Unit's own single component. This is deliberately NOT "shared by 2+ components"
    /// (an earlier, wrong heuristic this replaces - it only promoted a shared army-wide ability
    /// within a multi-component AttachedUnit, leaving it as an ordinary per-component entry on a
    /// standalone Unit, which is exactly as much an army-wide fact there): whether an ability
    /// gets this treatment is a structural property of the ability itself (its Origin), not a
    /// headcount of who happens to reference it in one particular roster. Multiple components
    /// referencing the same ArmyRule ability still collapse into one entry, same as before. Since
    /// this runs on a freshly-rebuilt list of only PRESENT components' abilities every request
    /// (this view is never cached), a component that dies simply stops contributing on the next
    /// rebuild - what makes the promoted entry's own collapse rule ("hidden once every
    /// contributing component is fully dead") fall out for free rather than needing separate
    /// tracking. Every other Origin (including CoreRule - a Core rule with no chapter exclusivity,
    /// e.g. "Deadly Demise") is untouched, exactly matching the existing "no cross-component
    /// combination" rule for everything else.</summary>
    private static IReadOnlyList<AggregateAbilityEntry> PromoteArmyRuleAbilities(List<AggregateAbilityEntry> entries)
    {
        var armyRuleGroups = entries
            .Where(e => e.Ability.Origin == AbilityOrigin.ArmyRule)
            .GroupBy(e => e.Ability.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var result = new List<AggregateAbilityEntry>();
        var promotedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (entry.Ability.Origin != AbilityOrigin.ArmyRule)
            {
                result.Add(entry);
                continue;
            }

            if (promotedNames.Add(entry.Ability.Name))
            {
                result.Add(entry with
                {
                    ComponentName = null,
                    ContributingComponentNames =
                    armyRuleGroups[entry.Ability.Name].Select(e => e.ComponentName!).ToList()
                });
            }
            // else: the promoted entry for this name was already added by an earlier contributor.
        }

        return result;
    }
}