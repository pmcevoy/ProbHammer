namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>Distinguishes an intrinsic, always-true fact about a Datasheet from an optional,
/// player-selectable ability grant - see Datasheet's On-Demand Ability Resolution and
/// BsdataDatasheetMapper's classification of a "type: upgrade" selection entry's own profiles.</summary>
public enum AbilityOrigin
{
    Intrinsic,
    Enhancement,
    OptionalGrant
}
