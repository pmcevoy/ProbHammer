using Microsoft.AspNetCore.Mvc.RazorPages;
using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Examples;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Web.Pages;

public class LivePlayModel : PageModel
{
    public List<UnitBlockViewModel> Units { get; private set; } = [];

    public void OnGet()
    {
        Units = View.MyArmy().Select(BuildUnitBlock).ToList();
    }

    private static UnitBlockViewModel BuildUnitBlock(AttachedUnitAggregateView view) =>
        new(
            Statlines: GroupStatlinesByValue(view.Statlines),
            Weapons: view.Weapons,
            UnitScopedAbilities: view.UnitScopedAbilities,
            ModelScopedAbilityGroups: GroupModelScopedAbilities(view.ModelScopedAbilities),
            Keywords: view.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList());

    // Display-only collapsing: statline entries with identical M/T/Sv/W/Ld/Oc render as one row
    // with their names stacked together. AttachedUnitAggregator still lists them as distinct
    // Statlines entries (deduped by name, not value) - this grouping is purely presentational.
    private static IReadOnlyList<StatlineRowViewModel> GroupStatlinesByValue(
        IReadOnlyList<AggregateStatlineEntry> statlines) =>
        statlines
            .GroupBy(entry => entry.Statline)
            .Select(group => new StatlineRowViewModel(
                Names: string.Join(" / ", group.Select(entry => entry.StatlineName)),
                Statline: group.Key))
            .ToList();

    private static IReadOnlyList<ModelAbilityGroupViewModel> GroupModelScopedAbilities(
        IReadOnlyList<ModelScopedAbilityEntry> entries) =>
        entries
            .GroupBy(entry => entry.ModelLine)
            .Select(group => new ModelAbilityGroupViewModel(
                ModelLineLabel: group.Key.StatlineName,
                Abilities: group.Select(entry => entry.Ability).ToList()))
            .ToList();
}

public sealed record StatlineRowViewModel(string Names, Statline Statline);

public sealed record ModelAbilityGroupViewModel(string ModelLineLabel, IReadOnlyList<Ability> Abilities);

public sealed record UnitBlockViewModel(
    IReadOnlyList<StatlineRowViewModel> Statlines,
    IReadOnlyList<AggregateWeaponEntry> Weapons,
    IReadOnlyList<Ability> UnitScopedAbilities,
    IReadOnlyList<ModelAbilityGroupViewModel> ModelScopedAbilityGroups,
    IReadOnlyList<string> Keywords);
