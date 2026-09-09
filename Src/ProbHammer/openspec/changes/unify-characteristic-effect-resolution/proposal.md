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

- `BsdataDatasheetMapper`/`BattleScribeRosterMapper` no longer classify a footnoted/split InSv value's
  linked ability text at parse time. When the raw characteristic text alone fully determines the value
  (a plain value, or a parenthetical melee/ranged restriction), that stays final, exactly as today.
  When it doesn't (a footnote or split naming a linked ability), the mapper still produces a caveated
  result carrying that ability — its own output shape is unchanged — it simply no longer attempts to
  interpret the ability's Text itself (no more `InvulnerableSaveCaveatClassifier` call).
- **Fixes the root-cause scoping bug directly**: the two internal ability-name conventions
  `ResolveCaveatAbility` looks for (`"Invulnerable Save ({N}+*)"`, `"*Invulnerable Save"`) are excluded
  from the general `Datasheet.Abilities`/`OptionalAbilityNames` walk, mirroring the existing
  `ExcludedAttachmentAbilityNames` exclusion (`"Leader"`/`"Support"`/`"Attached Unit"`) already used for
  the identical reason — every real corpus occurrence of these two name patterns is this internal
  mechanism, never a player-facing ability. Once excluded, the ability is never independently
  rediscovered at component-wide scope; `Statline.InSv`'s own existing per-Statline home is already
  correct with nothing left to leak into it. No new attachment-scope concept is introduced.
- **BREAKING (internal)**: `AttachedUnitAggregator.Build` gains one small, new resolution step: for
  every still-caveated `Statline.InSv`, attempt to resolve it via the checked-in
  `RuleClassificationBaseline` against its own single contributing ability, using the already-proven
  `InvulnerableSaveEffectResolver`. This step and `ApplyStatlineFlagRules` can no longer collide over
  the same field, since the exclusion above means the ability is never independently "present" for
  `ApplyStatlineFlagRules` to also match.
- **BREAKING (internal)**: `InvulnerableSaveCaveatClassifier` is deleted, superseded entirely by the
  above — safe only once `widen-baseline-generation-coverage` (Stage 1, hard prerequisite) closes the
  one real coverage gap (plural "have" phrasing) that classifier alone covers today.
- **BREAKING (internal)**: `CharacteristicModifierCandidate` stops being consumed at Build time at
  all — `AttachedUnitAggregator.ApplyCharacteristicModifierCandidates` is removed. Its own
  always-caveat, never-resolve behavior is retired, not preserved; a present ability whose
  structurally-derived Effect was captured into the baseline by Stage 1 now resolves through the
  ordinary present-ability pass (`ApplyStatlineFlagRules`) like everything else. Confirmed safe against
  the live corpus: every one of the 16 real tier-1 characteristic-modifier candidates today has real,
  matchable ability text reachable through the existing general ability-resolution walk (a local
  profile or an `infoLink`) — none depends on a raw-value-only resolution path that doesn't exist.
- InSv rejoins `CharacteristicModifierCandidate`'s own Field allowlist in `BsdataDatasheetMapper` — it
  was excluded specifically because nothing downstream could safely consume it without
  double-application risk; that reason no longer holds once there is one safe, unified consumer. Real
  corpus case this unlocks: Black Templars' "Consecrating Aura" Enhancement (tier-1, unconditional,
  currently discarded entirely).

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
- `invulnerable-save`: mapper-time resolution no longer attempts to classify a caveat's own linked
  ability text against any fixed template set — it defers entirely, and a new requirement gives every
  still-caveated invulnerable save one resolution attempt against the baseline during roster
  aggregation, using the exact same resolver `statline-flag-rules` already relies on.
- `characteristic-modifier-caveats`: retired. Its presence-gated, always-caveat, never-resolve
  Build-time application is removed outright — a present structural candidate now resolves (or
  produces no flagged value at all) through the ordinary `statline-flag-rules` present-ability pass,
  unchanged, not through a separate mechanism of its own.

`statline-flag-rules` itself is unaffected by this change — its own behavior doesn't change; it's the
mechanism the other two capabilities' own resolution now routes through unmodified.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs` — `ResolveInvulnerableSave`
  simplified to never classify a linked ability's text; InSv added to `CharacteristicFieldIds`; the two
  internal InSv-caveat-ability name conventions excluded from the general ability walk, mirroring
  `Datasheet`'s existing `ExcludedAttachmentAbilityNames` mechanism.
- `src/ProbHammer.Core/Domain/Import/BattleScribe/BattleScribeRosterMapper.cs` — its mirrored
  `ResolveInvulnerableSave` gets the identical simplification (the exclusion itself lives centrally in
  `Datasheet`'s own constructor, so both pipelines share it automatically).
- `src/ProbHammer.Core/Domain/Catalogue/InvulnerableSaveCaveatClassifier.cs` — deleted.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `ApplyCharacteristicModifierCandidates`
  removed; a new, small, InSv-specific step added that resolves any still-caveated `Statline.InSv`
  against the baseline. `ApplyStatlineFlagRules` itself is unmodified.
- `src/ProbHammer.Core/Domain/Catalogue/CharacteristicModifierCandidate.cs` — unaffected beyond what
  Stage 1 already added; InSv's own field-allowlist change lives in the mapper, not here.
- Test fixtures/tests referencing `InvulnerableSaveCaveatClassifier`,
  `ApplyCharacteristicModifierCandidates`, or mapper-time template resolution — updated or removed.
- No change to `/LivePlay` rendering — the flag marker/legend mechanism already reads
  `ContributingAbilities` generically, independent of what populates it.
