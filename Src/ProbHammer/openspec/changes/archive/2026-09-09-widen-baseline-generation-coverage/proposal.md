## Why

Two known, confirmed gaps sit in the checked-in `RuleClassificationBaseline`'s own coverage, found
while investigating a later consolidation of characteristic-effect resolution (a planned follow-up
change). First: `InvulnerableSaveCaveatClassifier`'s hand-rolled 4-template matcher (used only at
mapper time, for BSData/BattleScribe footnote resolution) covers a real corpus shape — "Models in
this unit have a {N}+ invulnerable save against {ranged|melee} attacks." (Howling Banshees) — that
`RuleEffectClassifier`'s own general patterns don't yet recognize, since they only match the singular
"has". That gap is a hard blocker: retiring the narrower mapper-time classifier in favor of the
general one, as the follow-up change intends, would silently regress Howling Banshees from resolved
to permanently caveated. Second: BSData sometimes encodes a characteristic grant structurally (a
`BsModifier` on the granting entry) rather than — or in addition to — describing it in prose the text
classifier can parse. Two real, confirmed examples (Auric Mantle, Consecrating Aura) are reachable
only this way today, and are either permanently caveated or thrown away entirely for want of a
derivation path. Closing both gaps grows the baseline's own trustworthy coverage with zero runtime
change, and removes the one concrete blocker standing in front of the later consolidation work.

## What Changes

- Widen `RuleEffectClassifier`'s `InvulnerableSaveRangedRestricted`/`InvulnerableSaveMeleeRestricted`
  patterns to also recognize a plural ("have") subject-verb form, alongside today's singular ("has"),
  mirroring `InvulnerableSaveCaveatClassifier`'s own existing bare/unit template pairing.
- Add a second, structural derivation path to the offline `RuleEffectClassificationReport` tool: for
  any corpus entry carrying both a resolvable Ability (Name+Text) and a tier-1/tier-2
  `CharacteristicModifierCandidate` targeting a recognized Statline field, mechanically derive a
  `CharacteristicEffect` directly from the modifier's own `{Field, Type, Value}` — no text parsing, no
  prose ambiguity.
- Add the raw-BSData-arithmetic → rulebook-`EffectVerb` inverse mapping this derivation needs (e.g. a
  raw `"increment"` on a `Plain` characteristic is rulebook `Improve`; the same raw `"increment"` on a
  `RollThreshold`/`ArmourPenetration` characteristic is rulebook `Worsen`) — reusing
  `CharacteristicModificationKind`'s existing arithmetic-family classification, in the opposite
  direction from its current verb-to-delta resolver.
- Where a structurally-derived Effect and a text-classified Effect both exist for the same Text, the
  report SHALL surface any disagreement between them as its own reviewable listing, rather than
  silently trusting either source.
- Re-run the corpus scan, review the newly-surfaced and changed output, and check in a grown baseline
  (`src/ProbHammer.Web/Data/RuleEffectClassifications.json`).

Explicitly out of scope for this change (reserved for the follow-up consolidation change):
- Consuming a structurally-derived Effect anywhere in `AttachedUnitAggregator` or the live app — this
  change only grows what the offline tool can produce and check in, never what the running app reads
  differently.
- Un-excluding InSv from `CharacteristicModifierCandidate`'s own Field allowlist in
  `BsdataDatasheetMapper` (Consecrating Aura stays excluded here, for the same "nothing downstream can
  safely consume it yet" reason it's excluded today).
- Any change to `InvulnerableSaveCaveatClassifier` itself, or to the mapper-time InSv resolution paths
  (`BsdataDatasheetMapper`/`BattleScribeRosterMapper`) that consult it.
- Any change to `CharacteristicModifierCandidate`'s own live, Build-time application
  (`AttachedUnitAggregator.ApplyCharacteristicModifierCandidates`).

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `rule-effect-classification`: widens the invulnerable-save-restricted Effect extraction requirement
  to also recognize a plural ("have") subject-verb form, not only the singular ("has") it recognizes
  today.
- `rule-effect-classification-baseline`: adds a second, structural derivation source for baseline
  entries — derived directly from a corpus entry's own `CharacteristicModifierCandidate`-shaped
  structured data rather than from its prose Text — cross-checked against the existing text-only
  classification wherever both exist for the same Text.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/RuleEffectClassifier.cs` — widen
  `InvulnerableSaveRangedRestricted`/`InvulnerableSaveMeleeRestricted` regex patterns to also match a
  plural lead-in.
- `tools/RuleEffectClassificationReport/` — new structural-derivation code path; a new report section
  for regex-vs-structural disagreement.
- `src/ProbHammer.Core/Domain/Catalogue/CharacteristicModificationKind.cs` (or a small new sibling) —
  the raw-arithmetic-to-rulebook-verb inverse mapping.
- `src/ProbHammer.Web/Data/RuleEffectClassifications.json` — grows with newly-captured entries from
  both sources.
- No change to `AttachedUnitAggregator`, `BsdataDatasheetMapper`, `BattleScribeRosterMapper`,
  `InvulnerableSaveCaveatClassifier`, or any other live/runtime code path.
