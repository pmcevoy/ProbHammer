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

    private static UnitBlockViewModel BuildUnitBlock(AttachedUnitAggregateView view)
    {
        var orderedWeapons = view.Weapons
            .OrderByDescending(w => w.TotalAttacks.ExpectedValue())
            .ToList();

        return new(
            Name: view.Name,
            Statlines: view.Statlines,
            RangedWeapons: orderedWeapons.Where(w => w.Profile.Type == WeaponType.Ranged).ToList(),
            MeleeWeapons: orderedWeapons.Where(w => w.Profile.Type == WeaponType.Melee).ToList(),
            UnitScopedAbilities: view.UnitScopedAbilities,
            ModelScopedAbilityGroups: GroupModelScopedAbilities(view.ModelScopedAbilities),
            Keywords: view.Keywords.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList());
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

public sealed record UnitBlockViewModel(
    string Name,
    IReadOnlyList<AggregateStatlineEntry> Statlines,
    IReadOnlyList<AggregateWeaponEntry> RangedWeapons,
    IReadOnlyList<AggregateWeaponEntry> MeleeWeapons,
    IReadOnlyList<Ability> UnitScopedAbilities,
    IReadOnlyList<ModelAbilityGroupViewModel> ModelScopedAbilityGroups,
    IReadOnlyList<string> Keywords);
