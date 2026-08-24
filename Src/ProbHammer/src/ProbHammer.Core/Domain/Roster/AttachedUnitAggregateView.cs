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
/// <see cref="StatlineName"/> is null for a Datasheet-sourced or Unit.Enhancements-sourced ability
/// (neither is tied to any one model-line - both apply to the whole component) and set for a
/// ModelLine-sourced ability (e.g. one granted by a wargear choice like Impulsor's "Shield Dome" -
/// tied to that specific model-line). This applies regardless of <see cref="Ability.Scope"/>:
/// Scope alone decides which UI column an entry belongs in; source alone decides which row(s) it
/// binds to. <see cref="Ability.Origin"/> (not this record's own shape) is what a renderer reads
/// to show an Enhancement-classified entry distinctly.
///
/// <see cref="ComponentName"/> is null only for a deduplicated Core Rule ability shared verbatim
/// by two or more present components of one AttachedUnit (e.g. "Templar Vows", identical by
/// Origin+Name across every component that references it) - see
/// <c>AttachedUnitAggregator.BuildAbilities</c>'s dedup step (gate-and-dedupe-core-rule-abilities).
/// Such an entry belongs to no single component, renders as its own row above every component's
/// statline rows, and always carries <see cref="StatlineName"/> null too (it can never be
/// row-bound - a shared army-wide fact is never tied to one specific model-line).
/// <see cref="ContributingComponentNames"/> is non-empty only in this case, listing every
/// component that contributed to the dedup - needed to evaluate this entry's own collapse rule
/// (hidden only once every one of those components is fully dead), which differs from the
/// single-component collapse rule every other entry uses.
/// </summary>
public sealed record AggregateAbilityEntry(
    string? ComponentName, string? StatlineName, Ability Ability, IReadOnlyList<string> ContributingComponentNames)
{
    public AggregateAbilityEntry(string ComponentName, string? StatlineName, Ability Ability)
        : this(ComponentName, StatlineName, Ability, [])
    {
    }
}

public sealed record AttachedUnitAggregateView(
    string Name,
    bool IsAttachedUnit,
    IReadOnlyList<AggregateStatlineEntry> Statlines,
    IReadOnlyList<AggregateWeaponEntry> Weapons,
    IReadOnlyList<AggregateAbilityEntry> Abilities,
    IReadOnlySet<string> Keywords);
