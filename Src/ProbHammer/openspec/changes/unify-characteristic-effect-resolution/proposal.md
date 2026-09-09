## Why

Investigating a real bug in `apply-rule-effect-baseline` (an InSv footnote-resolution ability getting
independently re-matched, at the wrong attachment scope, by the newer baseline-driven flag mechanism)
surfaced a structural problem, not just a local one: this codebase now has **three** independent
places that can derive a characteristic value from an ability or structural candidate —
`InvulnerableSaveCaveatClassifier` (mapper-time, 4 hand-rolled templates), the live
`CharacteristicModifierCandidate` application (`AttachedUnitAggregator.ApplyCharacteristicModifierCandidates`,
always-caveat, never-resolve), and the general `RuleClassificationBaseline`-driven flag pass
(`ApplyStatlineFlagRules`) — all predating each other, each narrower than the general one that now
exists, and coordinated only by an incidental "skip if already touched" guard rather than by design.
Confirmed by tracing: a second real ability (Auric Mantle) already collides across two of these three
mechanisms today, landing on the *correct* outcome only by accident of evaluation order — the exact
same accident that produces a *wrong* outcome for InSv. Consolidating into one resolution pass, fed by
one trust boundary (the baseline), removes the accident and the two narrower, now-redundant
mechanisms it grew around.

## What Changes

- **BREAKING (internal)**: `BsdataDatasheetMapper`/`BattleScribeRosterMapper` no longer classify a
  footnoted/split InSv value's linked ability text at parse time. When the raw characteristic text
  alone fully determines the value (a plain value, or a parenthetical melee/ranged restriction), that
  stays final, exactly as today. When it doesn't (a footnote or split naming a linked ability), the
  mapper records only that an ability exists and is associated with that specific characteristic at
  its own correct attachment scope — the one specific named Statline it was actually found on, never
  more broadly — with no judgment about whether it resolves or stays caveated. All resolution moves to
  `AttachedUnitAggregator.Build`.
- **BREAKING (internal)**: `InvulnerableSaveCaveatClassifier` is deleted, superseded entirely by the
  baseline-driven resolution `apply-rule-effect-baseline` already built — safe only once
  `widen-baseline-generation-coverage` (Stage 1, hard prerequisite) closes the one real coverage gap
  (plural "have" phrasing) that classifier alone covers today.
- **BREAKING (internal)**: `CharacteristicModifierCandidate` stops being consumed at Build time at
  all — `AttachedUnitAggregator.ApplyCharacteristicModifierCandidates` is removed. Its own
  always-caveat, never-resolve behavior is retired, not preserved; a present ability whose
  structurally-derived Effect was captured into the baseline by Stage 1 now resolves through the same
  single pass as everything else.
- InSv rejoins `CharacteristicModifierCandidate`'s own Field allowlist in `BsdataDatasheetMapper` — it
  was excluded specifically because nothing downstream could safely consume it without
  double-application risk; that reason no longer holds once there is one safe, unified consumer. Real
  corpus case this unlocks: Black Templars' "Consecrating Aura" Enhancement (tier-1, unconditional,
  currently discarded entirely).
- Fixes the root-cause scoping bug: a characteristic-pinned ability association is recorded once, at
  its own correct attachment scope, and is never also picked up a second time by the general
  ability-presence walk at a broader scope than it actually has.
- `AttachedUnitAggregator.Build`'s resolution pipeline collapses from three passes (ability-presence
  matching, structural-candidate caveating, and mapper-time InSv classification happening even earlier
  at parse time) into one, reading only the checked-in `RuleClassificationBaseline`.

Explicitly out of scope for this change:
- Building the evaluator for a keyword-targeted or unconditionally roster-wide effect (`RuleTarget`'s
  existing `Keyword`/`Unconditional` cases) — stays unevaluated, exactly as today. This change reserves
  room for that future "incoming, cross-unit" source without building it.
- Any change to `CharacteristicModifierCandidate`'s own classification logic in
  `BsdataDatasheetMapper` (`ClassifyCharacteristicModifierCandidates`, `IsTier1OrTier2`) beyond adding
  InSv to its Field allowlist — its tier-1/tier-2 recognition itself is unchanged.
- Anything from `widen-baseline-generation-coverage` (Stage 1) itself — this change depends on it
  being complete, not on redoing it.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `statline-flag-rules`: generalizes from "match a present ability's text against the baseline" to
  also resolve a characteristic-specific ability association a parser recorded (because it couldn't
  fully determine the value from raw text alone), at that association's own recorded attachment
  scope — the same baseline, the same resolution machinery, one additional input shape.
- `invulnerable-save`: mapper-time resolution no longer attempts to classify a caveat's own linked
  ability text against any fixed template set — it always defers full interpretation to the unified
  Build-time resolution instead.
- `characteristic-modifier-caveats`: retired. Its presence-gated, always-caveat, never-resolve
  Build-time application is removed outright — a present structural candidate now resolves (or stays
  caveated) through the same generalized `statline-flag-rules` mechanism as every other characteristic
  association, not through a separate mechanism of its own.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs` — `ResolveInvulnerableSave`
  simplified to never classify a linked ability's text; InSv added to `CharacteristicFieldIds`.
- `src/ProbHammer.Core/Domain/Import/BattleScribe/BattleScribeRosterMapper.cs` — its mirrored
  `ResolveInvulnerableSave` gets the identical simplification.
- `src/ProbHammer.Core/Domain/Catalogue/InvulnerableSaveCaveatClassifier.cs` — deleted.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `ApplyCharacteristicModifierCandidates`
  removed; `ApplyStatlineFlagRules` (or its successor) gains the new deferred-association input shape
  and takes over the correct-attachment-scope guarantee.
- `src/ProbHammer.Core/Domain/Catalogue/CharacteristicModifierCandidate.cs` — InSv-related field
  allowlist change lives in the mapper, not here; this type's own shape is unaffected beyond what
  Stage 1 already added.
- Test fixtures/tests referencing `InvulnerableSaveCaveatClassifier`,
  `ApplyCharacteristicModifierCandidates`, or mapper-time template resolution — updated or removed.
- No change to `/LivePlay` rendering — the flag marker/legend mechanism already reads
  `ContributingAbilities` generically, independent of what populates it.
