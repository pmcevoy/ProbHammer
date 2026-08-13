using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

public sealed record ModelLineLoadout(string WeaponsLabel, int RemainingCount, int InitialCount);

public sealed record AggregateStatlineEntry(
    string ComponentName,
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

/// <summary>
/// <see cref="StatlineName"/> is null for a Datasheet-sourced ability (not tied to any one
/// model-line - applies to the whole component) and set for a ModelLine-sourced ability
/// (Enhancement-conferred - tied to that specific model-line). This applies regardless of
/// <see cref="Ability.Scope"/>: Scope alone decides which UI column an entry belongs in; source
/// alone decides which row(s) it binds to.
/// </summary>
public sealed record AggregateAbilityEntry(string ComponentName, string? StatlineName, Ability Ability);

public sealed record AttachedUnitAggregateView(
    string Name,
    bool IsAttachedUnit,
    IReadOnlyList<AggregateStatlineEntry> Statlines,
    IReadOnlyList<AggregateWeaponEntry> Weapons,
    IReadOnlyList<AggregateAbilityEntry> Abilities,
    IReadOnlySet<string> Keywords);
