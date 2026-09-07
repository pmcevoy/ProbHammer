namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>A classified, data-derived characteristic-modifier candidate (see
/// characteristic-modifier-caveats and BsdataDatasheetMapper's own closed-world classifier) -
/// describes that <see cref="EntryName"/>, if actually selected on a resolved unit, structurally
/// modifies <see cref="Characteristic"/>. Never applied to this Datasheet's own Statline fields -
/// see Datasheet.CharacteristicModifierCandidates. <see cref="Characteristic"/> is one of "M", "T",
/// "Sv", "W", "Ld", "Oc" (Statline.M/T/Sv/W/Ld/Oc's own property names) - the only fields the
/// classifier's closed Field allowlist recognizes today; see the classifier's own doc comment for
/// why InSv and every WeaponProfile characteristic are deliberately excluded. <see cref="RawValue"/>
/// is the source BsModifier's own unparsed Value text, carried for a future DerivedValue-computing
/// step (proposal.md's "Explicitly deferred") - unused by this change's own caveat-only
/// application.</summary>
public sealed record CharacteristicModifierCandidate(string EntryName, string Characteristic, string RawValue);
