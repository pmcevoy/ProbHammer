using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Examples;
using ProbHammer.Core.Domain.Roster;

[assembly: InternalsVisibleTo("ProbHammer.Tests")]

namespace ProbHammer.Web.Pages;

public class LivePlayModel : PageModel
{
    public List<UnitBlockViewModel> Units { get; private set; } = [];

    public void OnGet()
    {
        Units = View.MyArmy()
            .OrderByDescending(v => v.IsAttachedUnit)
            .ThenByDescending(v => v.Statlines.Sum(s => s.InitialCount))
            .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .Select(BuildUnitBlock)
            .ToList();
    }

    internal static UnitBlockViewModel BuildUnitBlock(AttachedUnitAggregateView view)
    {
        var orderedWeapons = view.Weapons
            .OrderByDescending(w => w.TotalAttacks.ExpectedValue())
            .ToList();

        var statlineBlocks = GroupStatlines(view.Statlines, view.Abilities);

        return new(
            Name: view.Name,
            Statlines: statlineBlocks,
            RangedWeapons: orderedWeapons.Where(w => w.Profile.Type == WeaponType.Ranged).ToList(),
            MeleeWeapons: orderedWeapons.Where(w => w.Profile.Type == WeaponType.Melee).ToList(),
            ComponentAbilitySpans: BuildComponentAbilitySpans(statlineBlocks, view.Abilities),
            Keywords: view.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList());
    }

    // Single forward scan: a new run starts whenever ComponentName or Statline differs from the
    // running group's first entry (equivalently, from the immediately preceding entry, since a
    // group's entries always already match each other) - never a global by-value regroup, only
    // adjacency in the already-established component/declared-order sequence from BuildStatlines.
    // Row-bound (StatlineName != null) abilities attach to whichever run contains a matching
    // (ComponentName, StatlineName) entry, split into ModelAbilities/UnitAbilities by Scope.
    private static IReadOnlyList<StatlineBlockViewModel> GroupStatlines(
        IReadOnlyList<AggregateStatlineEntry> statlines, IReadOnlyList<AggregateAbilityEntry> abilities)
    {
        var groups = new List<List<AggregateStatlineEntry>>();

        foreach (var entry in statlines)
        {
            var currentGroup = groups.Count > 0 ? groups[^1] : null;
            if (currentGroup != null
                && currentGroup[0].ComponentName == entry.ComponentName
                && currentGroup[0].Statline == entry.Statline)
            {
                currentGroup.Add(entry);
            }
            else
            {
                groups.Add([entry]);
            }
        }

        return groups.Select(g => new StatlineBlockViewModel(
                Entries: g,
                ModelAbilities: RowBoundAbilities(g, abilities, AbilityScope.Model),
                UnitAbilities: RowBoundAbilities(g, abilities, AbilityScope.Unit)))
            .ToList();
    }

    private static IReadOnlyList<Ability> RowBoundAbilities(
        List<AggregateStatlineEntry> runEntries, IReadOnlyList<AggregateAbilityEntry> abilities, AbilityScope scope) =>
        abilities
            .Where(a => a.Ability.Scope == scope && a.StatlineName != null)
            .Where(a => runEntries.Any(e => e.ComponentName == a.ComponentName && e.StatlineName == a.StatlineName))
            .Select(a => a.Ability)
            .ToList();

    // Component-wide (Datasheet-sourced, StatlineName == null) abilities span every run belonging
    // to their component - from the index of that component's first rendered run through its last.
    private static IReadOnlyList<ComponentAbilitySpanViewModel> BuildComponentAbilitySpans(
        IReadOnlyList<StatlineBlockViewModel> statlineBlocks, IReadOnlyList<AggregateAbilityEntry> abilities) =>
        abilities
            .Where(a => a.StatlineName == null)
            .GroupBy(a => a.ComponentName)
            .Select(group =>
            {
                var runIndices = statlineBlocks
                    .Select((block, index) => (block, index))
                    .Where(x => x.block.Entries[0].ComponentName == group.Key)
                    .Select(x => x.index)
                    .ToList();

                return new ComponentAbilitySpanViewModel(
                    FirstRunIndex: runIndices.Min(),
                    LastRunIndex: runIndices.Max(),
                    ModelAbilities: group.Where(a => a.Ability.Scope == AbilityScope.Model).Select(a => a.Ability).ToList(),
                    UnitAbilities: group.Where(a => a.Ability.Scope == AbilityScope.Unit).Select(a => a.Ability).ToList());
            })
            .ToList();
}

/// <summary>A contiguous, same-component, value-identical run of statline entries sharing one
/// rendered stat-tile. <see cref="Entries"/> is never empty - every group is seeded with at least
/// one entry at creation. <see cref="ModelAbilities"/>/<see cref="UnitAbilities"/> are the
/// row-bound (ModelLine-sourced) abilities matching this specific run.</summary>
public sealed record StatlineBlockViewModel(
    IReadOnlyList<AggregateStatlineEntry> Entries,
    IReadOnlyList<Ability> ModelAbilities,
    IReadOnlyList<Ability> UnitAbilities)
{
    public Statline Statline => Entries[0].Statline;
}

/// <summary>A Datasheet-sourced (component-wide) ability group, rendered once beside the first of
/// its component's rendered runs and visually spanning through the last.</summary>
public sealed record ComponentAbilitySpanViewModel(
    int FirstRunIndex,
    int LastRunIndex,
    IReadOnlyList<Ability> ModelAbilities,
    IReadOnlyList<Ability> UnitAbilities);

public sealed record UnitBlockViewModel(
    string Name,
    IReadOnlyList<StatlineBlockViewModel> Statlines,
    IReadOnlyList<AggregateWeaponEntry> RangedWeapons,
    IReadOnlyList<AggregateWeaponEntry> MeleeWeapons,
    IReadOnlyList<ComponentAbilitySpanViewModel> ComponentAbilitySpans,
    IReadOnlyList<string> Keywords);
