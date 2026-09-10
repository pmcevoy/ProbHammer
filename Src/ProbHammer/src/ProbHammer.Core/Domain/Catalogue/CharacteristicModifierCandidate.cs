namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>A classified, data-derived characteristic-modifier candidate: describes that
/// <see cref="EntryName"/>, if actually selected on a resolved unit, structurally modifies
/// <see cref="Characteristic"/>. Never applied to this Datasheet's own Statline fields - see
/// Datasheet.CharacteristicModifierCandidates. <see cref="Characteristic"/> is one of "M", "T",
/// "Sv", "W", "Ld", "Oc", "InSv" (Statline.M/T/Sv/W/Ld/Oc/InSv's own property names) - the only
/// fields the classifier's closed Field allowlist recognizes; WeaponProfile characteristics are
/// deliberately excluded, see the classifier's own doc comment for why.
///
/// Classification-only data: not read by any Build-time resolution path. A present ability whose
/// granting selection also classifies as a CharacteristicModifierCandidate resolves (or doesn't)
/// purely through its own resolved Ability text against the checked-in baseline, the same as any
/// other characteristic-affecting ability. <see cref="RawValue"/> (the source BsModifier's
/// unparsed Value text) and <see cref="RawType"/> (its unparsed Type - "increment"/"decrement"/
/// "set") exist only for the offline report tool's structural derivation path
/// (<see cref="CharacteristicModificationResolver.ResolveVerbFromRawDelta"/>).</summary>
public sealed record CharacteristicModifierCandidate(
    string EntryName,
    string Characteristic,
    string RawValue,
    string RawType);