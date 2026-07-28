using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

public sealed record AggregateStatlineEntry(string StatlineName, Statline Statline);

public sealed record AggregateWeaponEntry(WeaponProfile Profile, int Count);

public sealed record ModelScopedAbilityEntry(ModelLine ModelLine, Ability Ability);

public sealed record AttachedUnitAggregateView(
    IReadOnlyList<AggregateStatlineEntry> Statlines,
    IReadOnlyList<AggregateWeaponEntry> Weapons,
    IReadOnlyList<Ability> UnitScopedAbilities,
    IReadOnlyList<ModelScopedAbilityEntry> ModelScopedAbilities,
    IReadOnlySet<string> Keywords);
