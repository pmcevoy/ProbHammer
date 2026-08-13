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
        // Loadouts is empty when exactly one ModelLine shares a statline name (the "no redundant
        // breakdown row" rule - see AggregateStatlineEntry), so Max(.,1) recovers the true
        // per-name ModelLine count in that case; summed across every entry, this gives the unit's
        // total ModelLine count even though BuildStatlines has already collapsed same-named lines
        // into one entry each. An AttachedUnit can never total 1 here (Bodyguard + >=1 Attached,
        // each with >=1 ModelLine, is >=2 by construction) - only a single-ModelLine plain Unit
        // (e.g. Impulsor) can.
        var totalModelLines = view.Statlines.Sum(s => Math.Max(s.Loadouts.Count, 1));

        var orderedWeapons = view.Weapons
            .OrderByDescending(w => w.TotalAttacks.ExpectedValue())
            .Select(w => new WeaponRowViewModel(w, BuildContributionBreakdown(w, totalModelLines)))
            .ToList();

        var statlineBlocks = GroupStatlines(view.Statlines, view.Abilities);

        return new(
            Name: view.Name,
            Statlines: statlineBlocks,
            RangedWeapons: orderedWeapons.Where(w => w.Entry.Profile.Type == WeaponType.Ranged).ToList(),
            MeleeWeapons: orderedWeapons.Where(w => w.Entry.Profile.Type == WeaponType.Melee).ToList(),
            ComponentAbilitySpans: BuildComponentAbilitySpans(statlineBlocks, view.Abilities),
            Keywords: view.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList());
    }

    // Rendering-only grouping over data AttachedUnitAggregator already computed correctly (mirrors
    // GroupStatlines/StatlineBlockViewModel's precedent - the domain retains ungrouped, per-
    // ModelLine provenance; only the display grouping is page-layer work). Groups Contributions by
    // (ComponentName, StatlineName) in first-seen order. A group collapses to one row when every
    // contribution shares the same PerModelAttacks; otherwise each contributing ModelLine renders
    // as its own row, so no subtotal is ever shown that isn't verifiable from the numbers beside it.
    // Label is StatlineName alone (no ComponentName prefix) - matches the validated UI shape and
    // accepts the same known limitation as ICombatUnit.Name's duplicate-leader case: two components
    // sharing a statline name would render identically-labeled rows.
    //
    // Trigger is the unit's total ModelLine count, not entry.Contributions.Count: knowing "this
    // came from the Sword Brother" is useful even when only one ModelLine carries a weapon, as
    // long as the unit has more than one ModelLine to distinguish it from. A unit with only one
    // ModelLine overall returns no rows for any of its weapons - there's no second source to name.
    internal static IReadOnlyList<WeaponContributionRow> BuildContributionBreakdown(
        AggregateWeaponEntry entry, int totalModelLinesInUnit)
    {
        if (totalModelLinesInUnit <= 1) return [];

        var groups = new List<List<WeaponContribution>>();
        var groupIndex = new Dictionary<(string ComponentName, string StatlineName), List<WeaponContribution>>();
        foreach (var contribution in entry.Contributions)
        {
            var key = (contribution.ComponentName, contribution.StatlineName);
            if (!groupIndex.TryGetValue(key, out var group))
            {
                group = [];
                groupIndex[key] = group;
                groups.Add(group);
            }
            group.Add(contribution);
        }

        var rows = new List<WeaponContributionRow>();
        foreach (var group in groups)
        {
            if (group.All(c => c.PerModelAttacks == group[0].PerModelAttacks))
            {
                var count = group.Sum(c => c.Count);
                rows.Add(new WeaponContributionRow(group[0].StatlineName, count, group[0].PerModelAttacks,
                    group[0].PerModelAttacks.Scale(count)));
            }
            else
            {
                rows.AddRange(group.Select(c =>
                    new WeaponContributionRow(c.StatlineName, c.Count, c.PerModelAttacks, c.PerModelAttacks.Scale(c.Count))));
            }
        }

        return rows;
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

/// <summary>One collapsed-or-expanded row of a weapon entry's contribution breakdown. Label is a
/// StatlineName (see <see cref="LivePlayModel.BuildContributionBreakdown"/> for the grouping
/// rule). Subtotal is always <c>PerModelAttacks.Scale(Count)</c> - safe to render standalone since
/// every row's own Count/PerModelAttacks are shown right beside it.</summary>
public sealed record WeaponContributionRow(string Label, int Count, DiceExpression PerModelAttacks, DiceExpression Subtotal);

/// <summary>A weapon entry plus its (possibly empty) contribution breakdown. Empty means the entry
/// has zero or one contributions - nothing to break down, so the page renders no toggle at all.</summary>
public sealed record WeaponRowViewModel(AggregateWeaponEntry Entry, IReadOnlyList<WeaponContributionRow> Breakdown);

public sealed record UnitBlockViewModel(
    string Name,
    IReadOnlyList<StatlineBlockViewModel> Statlines,
    IReadOnlyList<WeaponRowViewModel> RangedWeapons,
    IReadOnlyList<WeaponRowViewModel> MeleeWeapons,
    IReadOnlyList<ComponentAbilitySpanViewModel> ComponentAbilitySpans,
    IReadOnlyList<string> Keywords);
