using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

public sealed record ModelLineLoadout(
    string WeaponsLabel,
    IReadOnlyList<string> Weapons,
    int RemainingCount,
    int InitialCount,
    IReadOnlyList<string> Abilities,
    string DisplayName);

/// <summary><see cref="NotAppliedEffects"/> lists every conditional Statline scalar or
/// invulnerable-save effect reaching this entry, left unapplied.</summary>
public sealed record AggregateStatlineEntry(
    string ComponentName,
    string StatlineName,
    Statline Statline,
    int RemainingCount,
    int InitialCount,
    IReadOnlyList<ModelLineLoadout> Loadouts,
    IReadOnlyList<NotAppliedStatlineEffect>? NotAppliedEffects = null)
{
    public IReadOnlyList<NotAppliedStatlineEffect> NotAppliedEffects { get; init; } = NotAppliedEffects ?? [];
}

/// <summary>One matched, non-caveated Attacks-characteristic effect reaching a
/// <see cref="WeaponContribution"/> - the source ability plus its own resolved signed per-model
/// amount (<see cref="CharacteristicModificationResolver.ResolveAttacksAmount"/>). Never folded
/// into <see cref="WeaponContribution.PerModelAttacks"/> - see that record's own doc comment for
/// why Attacks can't reuse S/AP/D's mutate-in-place convention.</summary>
public sealed record AttacksContribution(Ability SourceAbility, int Amount);

/// <summary>Why a conditional effect was not applied: the record's usage limit and turn ownership, the
/// effect's own condition text, and whether it is one branch of a choice.</summary>
public sealed record EffectCondition(
    UsageLimit? UsageLimit,
    GameTurn? TurnOwnership,
    string? ConditionText,
    bool IsChoiceBranch)
{
    public static EffectCondition Of(AbilityClassification classification, ClassifiedEffect effect) =>
        new(classification.UsageLimit, classification.TurnOwnership, effect.ConditionText,
            effect.ChoiceBranch is not null);
}

/// <summary>A conditional weapon-characteristic effect reaching a contribution but left unapplied.
/// <see cref="Amount"/> is the signed per-model change it would make (AP improve 1 is -1), or the
/// assigned value when <see cref="Verb"/> is Set.</summary>
public sealed record NotAppliedWeaponEffect(
    Ability SourceAbility,
    string Characteristic,
    EffectVerb Verb,
    int Amount,
    EffectCondition Condition);

/// <summary>An unconditional weapon keyword grant that changed a contribution's keywords: it added
/// <see cref="Keyword"/>, replacing <see cref="ReplacedKeyword"/> when it was a better value of a
/// keyword the weapon already had.</summary>
public sealed record KeywordGrant(Ability SourceAbility, string Keyword, string? ReplacedKeyword);

/// <summary>A conditional weapon keyword grant reaching a contribution that would change its
/// keywords, left unapplied.</summary>
public sealed record NotAppliedKeywordGrant(Ability SourceAbility, string Keyword, EffectCondition Condition);

/// <summary>A conditional Statline scalar or invulnerable-save effect reaching an entry but left
/// unapplied.</summary>
public sealed record NotAppliedStatlineEffect(
    Ability SourceAbility,
    RuleEffect Effect,
    EffectCondition Condition);

/// <summary>
/// <see cref="LoadoutIndex"/> is the contributing <c>ModelLine</c>'s position within its
/// statline's <see cref="AggregateStatlineEntry.Loadouts"/> list (same ordering
/// <c>AttachedUnitAggregator.BuildStatlines</c> already establishes) - lets a consumer correlate a
/// specific contribution to a specific loadout unambiguously, since two sibling loadouts under the
/// same statline name are otherwise indistinguishable by <see cref="ComponentName"/>/
/// <see cref="StatlineName"/> alone, and <see cref="Count"/> is not reliable (two loadouts can
/// coincidentally share a model count). <c>-1</c> when the statline has only one <c>ModelLine</c>
/// (no <c>Loadouts</c> rendered at all, so there is nothing to index).
/// <see cref="NotAppliedEffects"/> lists every present, bearer-scoped, selector-matched conditional
/// weapon effect - matched the same way an applied <c>WeaponCharacteristicEffect</c> would be, but
/// left unapplied. Empty when no conditional match reaches this contribution. Independent of whether this
/// contribution's own <see cref="PerModelAttacks"/>/<see cref="Name"/> already reflect an applied
/// mutation from a different, non-caveated ability.
/// <see cref="AttacksContributions"/> is Attacks' own genuinely different mechanism
/// (`resolve-weapon-attacks-effects`): unlike S/AP/D, a matched Attacks effect never mutates
/// <see cref="PerModelAttacks"/> in place (Attacks is excluded from
/// <see cref="WeaponProfile.EqualityKey()"/>'s grouping identity, so mutating it in place would
/// discard the per-ability attribution a consumer needs) - it's recorded here instead, empty when
/// no non-caveated Attacks match reaches this contribution.
/// <see cref="KeywordGrants"/> are the applied grants already folded into the contribution's
/// keywords; <see cref="NotAppliedKeywordGrants"/> the conditional ones that would change them.
/// </summary>
public sealed record WeaponContribution(
    string ComponentName,
    string StatlineName,
    int Count,
    DiceExpression PerModelAttacks,
    string Name,
    int LoadoutIndex = -1,
    IReadOnlyList<NotAppliedWeaponEffect>? NotAppliedEffects = null,
    IReadOnlyList<AttacksContribution>? AttacksContributions = null,
    IReadOnlyList<KeywordGrant>? KeywordGrants = null,
    IReadOnlyList<NotAppliedKeywordGrant>? NotAppliedKeywordGrants = null)
{
    public IReadOnlyList<NotAppliedWeaponEffect> NotAppliedEffects { get; init; } = NotAppliedEffects ?? [];
    public IReadOnlyList<AttacksContribution> AttacksContributions { get; init; } = AttacksContributions ?? [];
    public IReadOnlyList<KeywordGrant> KeywordGrants { get; init; } = KeywordGrants ?? [];

    public IReadOnlyList<NotAppliedKeywordGrant> NotAppliedKeywordGrants { get; init; } =
        NotAppliedKeywordGrants ?? [];
}

/// <summary>
/// <see cref="Profile"/> is retained for its identity fields (Type/Range/Skill/S/Ap/D/ability
/// flags) - but once a row merges contributions from multiple model-lines, <c>Profile.A</c> and
/// <c>Profile.Name</c> are each whichever contributor happened to be inserted first and are not
/// authoritative. Only <see cref="TotalAttacks"/> and <see cref="Name"/> are safe to render.
/// <see cref="NotAppliedEffects"/> is every contribution's own
/// <see cref="WeaponContribution.NotAppliedEffects"/>, distinct by source ability and
/// characteristic, in first-encountered order. <see cref="KeywordGrants"/> and
/// <see cref="NotAppliedKeywordGrants"/> are likewise every contribution's own, distinct by source
/// ability and keyword.
/// </summary>
public sealed record AggregateWeaponEntry(
    WeaponProfile Profile,
    DiceExpression TotalAttacks,
    string Name,
    IReadOnlyList<WeaponContribution> Contributions,
    IReadOnlyList<NotAppliedWeaponEffect>? NotAppliedEffects = null,
    IReadOnlyList<KeywordGrant>? KeywordGrants = null,
    IReadOnlyList<NotAppliedKeywordGrant>? NotAppliedKeywordGrants = null)
{
    public IReadOnlyList<NotAppliedWeaponEffect> NotAppliedEffects { get; init; } = NotAppliedEffects ?? [];
    public IReadOnlyList<KeywordGrant> KeywordGrants { get; init; } = KeywordGrants ?? [];

    public IReadOnlyList<NotAppliedKeywordGrant> NotAppliedKeywordGrants { get; init; } =
        NotAppliedKeywordGrants ?? [];
}

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
/// <c>AttachedUnitAggregator.BuildAbilities</c>'s dedup step.
/// Such an entry belongs to no single component, renders as its own row above every component's
/// statline rows, and always carries <see cref="StatlineName"/> null too (it can never be
/// row-bound - a shared army-wide fact is never tied to one specific model-line).
/// <see cref="ContributingComponentNames"/> is non-empty only in this case, listing every
/// component that contributed to the dedup - needed to evaluate this entry's own collapse rule
/// (hidden only once every one of those components is fully dead), which differs from the
/// single-component collapse rule every other entry uses.
/// </summary>
public sealed record AggregateAbilityEntry(
    string? ComponentName,
    string? StatlineName,
    Ability Ability,
    IReadOnlyList<string> ContributingComponentNames)
{
    public AggregateAbilityEntry(string ComponentName, string? StatlineName, Ability Ability)
        : this(ComponentName, StatlineName, Ability, [])
    {
    }
}

/// <summary>A conditional slot of an ability on a unit that the player can activate, reported only
/// when one of its effects reaches a Statline entry or weapon contribution of the unit.</summary>
public abstract record ActivatableCondition(Ability Ability);

/// <summary>Every non-choice conditional effect of <see cref="ActivatableCondition.Ability"/> sharing
/// <see cref="ConditionText"/> - the empty string when only a usage limit or turn restriction makes
/// it conditional.</summary>
public sealed record ConditionToggle(
    Ability Ability,
    string ConditionText,
    UsageLimit? UsageLimit,
    GameTurn? TurnOwnership,
    bool IsActive) : ActivatableCondition(Ability);

/// <summary>One choice group of <see cref="ActivatableCondition.Ability"/> and its selected option
/// indexes.</summary>
public sealed record ChoiceToggle(
    Ability Ability,
    int GroupIndex,
    ChoiceGroup Group,
    IReadOnlySet<int> Selected) : ActivatableCondition(Ability);

public sealed record AttachedUnitAggregateView(
    string Name,
    bool IsAttachedUnit,
    IReadOnlyList<AggregateStatlineEntry> Statlines,
    IReadOnlyList<AggregateWeaponEntry> Weapons,
    IReadOnlyList<AggregateAbilityEntry> Abilities,
    IReadOnlySet<string> Keywords,
    IReadOnlyList<ActivatableCondition>? ActivatableConditions = null)
{
    public IReadOnlyList<ActivatableCondition> ActivatableConditions { get; init; } = ActivatableConditions ?? [];
}