namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>A classified, data-derived characteristic-modifier candidate (see
/// characteristic-modifier-caveats and BsdataDatasheetMapper's own closed-world classifier) -
/// describes that <see cref="EntryName"/>, if actually selected on a resolved unit, structurally
/// modifies <see cref="Characteristic"/>. Never applied to this Datasheet's own Statline fields -
/// see Datasheet.CharacteristicModifierCandidates. <see cref="Characteristic"/> is one of "M", "T",
/// "Sv", "W", "Ld", "Oc", "InSv" (Statline.M/T/Sv/W/Ld/Oc/InSv's own property names) - the only
/// fields the classifier's closed Field allowlist recognizes today; see the classifier's own doc
/// comment for why every WeaponProfile characteristic is deliberately excluded.
/// <see cref="RawValue"/> is the source BsModifier's own unparsed Value text, carried for a future
/// DerivedValue-computing step (proposal.md's "Explicitly deferred") - not consumed by anything
/// today. <see cref="RawType"/> is the source BsModifier's own unparsed Type
/// ("increment"/"decrement"/"set") - added by widen-baseline-generation-coverage for the offline
/// report tool's structural derivation path (see CharacteristicModificationResolver
/// .ResolveVerbFromRawDelta); purely additive, likewise unconsumed at Build time. Since
/// unify-characteristic-effect-resolution retired AttachedUnitAggregator's own live consumer of
/// this type entirely, every field here is classification-only data - not read by any Build-time
/// resolution path; a present ability whose granting selection also classifies as a
/// CharacteristicModifierCandidate now resolves (or doesn't) purely through its own resolved
/// Ability text against the checked-in baseline, the same as any other characteristic-affecting
/// ability.</summary>
public sealed record CharacteristicModifierCandidate(
    string EntryName,
    string Characteristic,
    string RawValue,
    string RawType);