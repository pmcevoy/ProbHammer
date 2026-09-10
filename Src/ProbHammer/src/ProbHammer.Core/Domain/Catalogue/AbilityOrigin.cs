namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Distinguishes an intrinsic, always-true fact about a Datasheet from an optional,
/// player-selectable ability grant - see Datasheet's On-Demand Ability Resolution and
/// BsdataDatasheetMapper's classification of a "type: upgrade" selection entry's own profiles.</summary>
public enum AbilityOrigin
{
    Intrinsic,
    Enhancement,
    OptionalGrant,

    /// <summary>A datasheet-wide reference to a separately-defined Core rule (e.g. Deadly Demise,
    /// Firing Deck, Infiltrators) - see BsdataDatasheetMapper's resolution of a "type: rule"
    /// infoLink via RuleGlossary. Always-exposed, like Intrinsic, not one of the on-demand
    /// optional grants. Distinguished from <see cref="ArmyRule"/> purely by name, not by gating
    /// shape: several mustering/composition rules (e.g. Assigned Agents) share the same
    /// primary-catalogue-scoped gating as a genuine army rule without being one, so the resolved
    /// rule's own Name is checked against <c>ArmyRuleNameLookup.Resolve(Faction)</c>'s curated
    /// per-faction table instead - CoreRule otherwise. See ArmyRuleNameLookup's own doc
    /// comment.</summary>
    CoreRule,

    /// <summary>A datasheet-wide reference to a Core rule that IS one of the roster's Faction's
    /// known army-wide rules (Oath of Moment, a Chapter's own Vows, Nurgle's Gift (Aura), etc. -
    /// see <see cref="CoreRule"/> for how the distinction is made). Unlike every other Origin, an
    /// ArmyRule ability shared by multiple present components of one AttachedUnit is deduplicated
    /// into a single entry belonging to no component (see
    /// AttachedUnitAggregator.PromoteArmyRuleAbilities) and rendered in its own dedicated row,
    /// even for a standalone Unit with only one component - since it's an army-wide fact
    /// regardless of how many components in a given roster happen to reference it, not merely
    /// "shared by coincidence."</summary>
    ArmyRule
}