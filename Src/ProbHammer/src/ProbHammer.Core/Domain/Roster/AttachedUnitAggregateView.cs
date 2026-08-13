using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

public sealed record ModelLineLoadout(string WeaponsLabel, int RemainingCount, int InitialCount);

public sealed record AggregateStatlineEntry(
    string StatlineName,
    Statline Statline,
    int RemainingCount,
    int InitialCount,
    IReadOnlyList<ModelLineLoadout> Loadouts);

public sealed record WeaponContribution(
    string ComponentName,
    string StatlineName,
    int Count,
    DiceExpression PerModelAttacks);

/// <summary>
/// <see cref="Profile"/> is retained for its identity fields (Name/Type/Range/Skill/S/Ap/D/ability
/// flags) - but once a row merges contributions from multiple model-lines, <c>Profile.A</c> is
/// whichever contributor happened to be inserted first and is not authoritative. Only
/// <see cref="TotalAttacks"/> is safe to render.
/// </summary>
public sealed record AggregateWeaponEntry(
    WeaponProfile Profile,
    DiceExpression TotalAttacks,
    IReadOnlyList<WeaponContribution> Contributions);

public sealed record ModelScopedAbilityEntry(ModelLine ModelLine, Ability Ability);

public sealed record AttachedUnitAggregateView(
    string Name,
    IReadOnlyList<AggregateStatlineEntry> Statlines,
    IReadOnlyList<AggregateWeaponEntry> Weapons,
    IReadOnlyList<Ability> UnitScopedAbilities,
    IReadOnlyList<ModelScopedAbilityEntry> ModelScopedAbilities,
    IReadOnlySet<string> Keywords);
