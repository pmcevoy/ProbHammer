namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Distinguishes an intrinsic, always-true fact about a Datasheet from an optional,
/// player-selectable ability grant - see Datasheet's On-Demand Ability Resolution and
/// BsdataDatasheetMapper's classification of a "type: upgrade" selection entry's own profiles.</summary>
public enum AbilityOrigin
{
    Intrinsic,
    Enhancement,
    OptionalGrant,

    /// <summary>A datasheet-wide reference to a separately-defined Core rule with no chapter/
    /// sub-faction exclusivity of its own (e.g. Deadly Demise, Firing Deck, Infiltrators - true
    /// for every datasheet that references them, regardless of army) - see
    /// BsdataDatasheetMapper's resolution of a "type: rule" infoLink via RuleGlossary.
    /// Always-exposed, like Intrinsic, not one of the on-demand optional grants. Distinguished
    /// from <see cref="ArmyRule"/> by whether the referenced rule's own gating carries a
    /// "primary-catalogue" (chapter/sub-faction) condition - a structural fact about the rule
    /// itself, not a guess.</summary>
    CoreRule,

    /// <summary>A datasheet-wide reference to a Core rule whose own gating is chapter/sub-faction
    /// exclusive (confirmed real shapes: Oath of Moment hidden unless the army is one of 11 named
    /// chapters; a Chapter's own Vows hidden unless it specifically is that one chapter) - the
    /// same underlying BSData shape as <see cref="CoreRule"/>, distinguished only by that gating
    /// signal. Unlike every other Origin, an ArmyRule ability shared by multiple present
    /// components of one AttachedUnit is deduplicated into a single entry belonging to no
    /// component (see AttachedUnitAggregator.PromoteArmyRuleAbilities) and rendered in its own
    /// dedicated row, even for a standalone Unit with only one component - since it's an
    /// army-wide fact regardless of how many components in a given roster happen to reference
    /// it, not merely "shared by coincidence."</summary>
    ArmyRule
}
