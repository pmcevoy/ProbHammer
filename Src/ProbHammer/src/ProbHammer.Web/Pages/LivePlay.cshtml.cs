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

        return new(
            Name: view.Name,
            Statlines: GroupStatlines(view.Statlines),
            RangedWeapons: orderedWeapons.Where(w => w.Profile.Type == WeaponType.Ranged).ToList(),
            MeleeWeapons: orderedWeapons.Where(w => w.Profile.Type == WeaponType.Melee).ToList(),
            UnitScopedAbilities: view.UnitScopedAbilities,
            ModelScopedAbilityGroups: GroupModelScopedAbilities(view.ModelScopedAbilities),
            Keywords: view.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList());
    }

    // Single forward scan: a new run starts whenever ComponentName or Statline differs from the
    // running group's first entry (equivalently, from the immediately preceding entry, since a
    // group's entries always already match each other) - never a global by-value regroup, only
    // adjacency in the already-established component/declared-order sequence from BuildStatlines.
    private static IReadOnlyList<StatlineBlockViewModel> GroupStatlines(
        IReadOnlyList<AggregateStatlineEntry> statlines)
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

        return groups.Select(g => new StatlineBlockViewModel(g)).ToList();
    }

    private static IReadOnlyList<ModelAbilityGroupViewModel> GroupModelScopedAbilities(
        IReadOnlyList<ModelScopedAbilityEntry> entries) =>
        entries
            .GroupBy(entry => entry.ModelLine)
            .Select(group => new ModelAbilityGroupViewModel(
                ModelLineLabel: group.Key.StatlineName,
                Abilities: group.Select(entry => entry.Ability).ToList()))
            .ToList();
}

public sealed record ModelAbilityGroupViewModel(string ModelLineLabel, IReadOnlyList<Ability> Abilities);

/// <summary>A contiguous, same-component, value-identical run of statline entries sharing one
/// rendered stat-tile. <see cref="Entries"/> is never empty - every group is seeded with at least
/// one entry at creation.</summary>
public sealed record StatlineBlockViewModel(IReadOnlyList<AggregateStatlineEntry> Entries)
{
    public Statline Statline => Entries[0].Statline;
}

public sealed record UnitBlockViewModel(
    string Name,
    IReadOnlyList<StatlineBlockViewModel> Statlines,
    IReadOnlyList<AggregateWeaponEntry> RangedWeapons,
    IReadOnlyList<AggregateWeaponEntry> MeleeWeapons,
    IReadOnlyList<Ability> UnitScopedAbilities,
    IReadOnlyList<ModelAbilityGroupViewModel> ModelScopedAbilityGroups,
    IReadOnlyList<string> Keywords);
