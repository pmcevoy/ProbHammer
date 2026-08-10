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
            Statlines: view.Statlines,
            Weapons: view.Weapons,
            UnitScopedAbilities: view.UnitScopedAbilities,
            ModelScopedAbilityGroups: GroupModelScopedAbilities(view.ModelScopedAbilities),
            Keywords: view.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList());

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

public sealed record UnitBlockViewModel(
    IReadOnlyList<AggregateStatlineEntry> Statlines,
    IReadOnlyList<AggregateWeaponEntry> Weapons,
    IReadOnlyList<Ability> UnitScopedAbilities,
    IReadOnlyList<ModelAbilityGroupViewModel> ModelScopedAbilityGroups,
    IReadOnlyList<string> Keywords);
