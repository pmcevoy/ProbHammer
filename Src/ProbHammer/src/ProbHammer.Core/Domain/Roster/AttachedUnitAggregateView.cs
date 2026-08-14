using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

public sealed record ModelLineLoadout(
    string WeaponsLabel, IReadOnlyList<string> Weapons, int RemainingCount, int InitialCount);

public sealed record AggregateStatlineEntry(
    string ComponentName,
    string StatlineName,
    Statline Statline,
    int RemainingCount,
    int InitialCount,
    IReadOnlyList<ModelLineLoadout> Loadouts);

/// <summary>
/// <see cref="LoadoutIndex"/> is the contributing <c>ModelLine</c>'s position within its
/// statline's <see cref="AggregateStatlineEntry.Loadouts"/> list (same ordering
/// <c>AttachedUnitAggregator.BuildStatlines</c> already establishes) - lets a consumer correlate a
/// specific contribution to a specific loadout unambiguously, since two sibling loadouts under the
/// same statline name are otherwise indistinguishable by <see cref="ComponentName"/>/
/// <see cref="StatlineName"/> alone, and <see cref="Count"/> is not reliable (two loadouts can
/// coincidentally share a model count). <c>-1</c> when the statline has only one <c>ModelLine</c>
/// (no <c>Loadouts</c> rendered at all, so there is nothing to index).
/// </summary>
public sealed record WeaponContribution(
    string ComponentName,
    string StatlineName,
    int Count,
    DiceExpression PerModelAttacks,
    int LoadoutIndex = -1);

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
