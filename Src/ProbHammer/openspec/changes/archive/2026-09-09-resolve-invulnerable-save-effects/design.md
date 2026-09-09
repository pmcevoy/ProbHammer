## Context

See proposal.md - Why. `CharacteristicEffect` (`Src/ProbHammer.Core/Domain/Catalogue/CharacteristicEffect.cs`)
is today a single sealed `(Characteristic, Verb, Amount)` record; `RuleEffectClassifier`'s
`InvulnerableSaveGrant` pattern already extracts a uniform "Set InSv N" grant into it, but excludes
any attack-type-restricted phrasing via a negative lookahead. `CharacteristicModificationKinds`
explicitly throws for `"InSv"`, since it's a compound `InvulnerableSave(Melee, Ranged)` wrapped in
`InvulnerableSaveCharacteristicView`, not a plain `CharacteristicValue` — so nothing today can resolve
a classified InSv Effect into a real Statline value; `ShieldDomeStatlineFlagRule` remains the only
hand-authored path that does.

## Goals / Non-Goals

**Goals:**
- Extend `RuleEffectClassifier` to extract melee/ranged-restricted invulnerable-save grants (both
  one-sided and two-sided), replacing today's exclusion.
- Retype `CharacteristicEffect` so InSv's compound value can be represented as a real element of the
  existing `Effects` list, not a sibling field.
- Build a standalone, provably-correct-in-isolation resolver from a classified InSv Effect + its
  source `Ability` to a real `InvulnerableSaveCharacteristicView`.
- Add a cheap, provably-safe pre-filter gate ahead of the InSv pattern family, and use it to sharpen
  the corpus report tool's default-only review bucket.
- Migrate the existing baseline's InSv-carrying entries to the new discriminated shape.

**Non-Goals:**
- Wiring the new resolver into `AttachedUnitAggregator`/`StatlineFlagRuleCatalogue`.
  `ShieldDomeStatlineFlagRule` is untouched by this change.
- `KeywordEffect`/`AbilityEffect` sibling types (see proposal.md's Non-Goals — named, deferred).
- Modeling the Psychic-Attack/Daemon-attack-source restriction axis in any form — no `/LivePlay`
  representation exists for it today.
- Redesigning `RuleEffectClassifier`'s own architecture into a composed pipeline of independently-
  gated family classifiers — see Open Questions.

## Decisions

**D1. `CharacteristicEffect` becomes abstract with `ScalarCharacteristicEffect` (today's shape,
renamed) and `InvulnerableSaveCharacteristicEffect(InvulnerableSave Value)` sealed subtypes, staying
inside the existing `Effects` list.** `Effects` is already a variable-length list — the correct
generalization axis is the *element* type, not a sibling field on `RuleClassification`, mirroring
`WeaponProfile`/`CharacteristicValue`/`RuleTarget`'s existing abstract-base/sealed-subtype convention.
Rejected alternative: a separate top-level `RuleClassification` field for InSv. That would also walk
straight into an already-documented, confirmed gap (`vnext-ideas.md`'s "schema-growth" note) where a
brand-new *top-level* field on `RuleClassification` always serializes present-at-default, breaking the
baseline's new/drift/unchanged detection. A new element inside an already-list-shaped field doesn't
hit that gap — a text going from `effects: []` to `effects: [<new element>]` is legitimate, correctly-
flagged new content, exactly how every other newly-recognized pattern in
`widen-rule-effect-classification-coverage` already surfaced. It also means the existing
`IsCaveated` gate (`effects.Count > 0`) needs no change at all — InSv participating fully in `Effects`
is what makes Ensorcelled Shield's own "and the Feel No Pain 6+ ability" trailing content get caught
by the same, already-correct mechanism.

**D2. `InvulnerableSaveCharacteristicEffect` carries a plain `InvulnerableSave` value, no `Verb`.**
Every real corpus InSv grant on the melee/ranged axis this change covers is `Set`-shaped; the one
confirmed `Improve`-verbed InSv example found in the corpus ("...invulnerable save is improved to 4+
against Psychic Attacks") is on the already-excluded Psychic-Attacks axis. Within the axis this change
actually extracts, `Set`-only is the evidenced shape, not a speculative accommodation. If a genuine
`Improve`/`Worsen` InSv phrasing on the melee/ranged axis is ever found, that's real future work.

**D3. A one-sided restriction is represented as `0` on the untargeted side**, reusing
`InvulnerableSave.None`'s existing sentinel and `InvulnerableSaveCaveatClassifier.TryResolveBare`'s
identical established convention, rather than a nullable-int pair or a separate "restricted" flag. `0`
is a safe, unambiguous sentinel — no real 11e invulnerable save is ever stated as `0+` or `1+`.

**D4. Two independent per-side search patterns (ranged-restricted, melee-restricted), combined into
one `InvulnerableSave`**, rather than one paired-template regex — covers both the one-sided
(Ensorcelled Shield) and two-sided (Veil of Medrengard) real shapes with one mechanism, whichever
side(s) match. Kept structurally separate from `InvulnerableSaveCaveatClassifier`'s own four anchored
whole-string templates: that classifier resolves an already-isolated footnote's linked ability text
via an exact whole-string match; this one searches within arbitrary free prose and needs the same
`SentenceStart`/subject-shaped-prefix anchoring `InvulnerableSaveGrant` already uses. No code sharing
attempted between the two — they solve structurally different problems.

**D5. A cheap, provably-safe pre-filter gate runs ahead of the InSv pattern family**: the normalized
text must contain the literal substring "invulnerable save" (case-insensitive). Every InSv pattern —
existing and new — already requires that substring to match at all, so the gate is a strict
over-approximation: it can never reject a text any InSv pattern would have accepted, checkable by
inspection of the patterns themselves rather than by corpus-running-and-hoping. Scoped narrowly to the
InSv family only in this change, not a general "every family gets a gate" redesign (see D8/Open
Questions).

**D6. The corpus report tool's default-only bucket splits using the D5 gate**: a text the gate rejects
outright is excluded from the reviewable listing entirely (zero review value, provably so); a text
that passes the gate but matches no InSv pattern stays in a distinct, reviewable listing — turning an
unreviewable ~3000-entry bucket into a small, high-signal one for this family.

**D7. JSON serialization uses `[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]`/
`[JsonDerivedType]` on `CharacteristicEffect`**, mirroring `RuleTarget`'s existing convention exactly —
not a new serialization pattern for this codebase. `InvulnerableSave` itself needs no new attributes;
the existing camelCase policy on `RuleClassificationBaseline.Options` covers it.

**D8. `RuleEffectClassifier`'s own architecture is explicitly not redesigned in this change** — see
Open Questions. The gate built under D5 is scoped narrowly enough to fit either a future
per-family-pipeline redesign or staying as-is; nothing here forecloses that later decision.

## Risks / Trade-offs

- [Risk] Going polymorphic on `CharacteristicEffect` is a breaking public-API and JSON-shape change.
  → [Mitigation] Every call site is inside this project's own Core/Web/Tests assemblies — no external
  consumer. The non-InSv portion of the baseline migration is mechanical via `--write-baseline`.
- [Risk] Manually re-reviewing the ~10-15 already-baselined InSv entries under the new shape risks a
  transcription slip. → [Mitigation] `--write-baseline` regenerates the mechanical `"kind"` field
  automatically; only the reshaped `value` needs a human look, each checkable directly against its own
  source text.
- [Risk] A future corpus text could combine a melee/ranged restriction with a Psychic/Daemon
  restriction in the same clause (not observed, but plausible — e.g. "...against ranged Psychic
  Attacks"). → [Mitigation] The new patterns' own literal "ranged"/"melee" word requirements make an
  accidental match on such a text unlikely; add a corpus-scan regression case for this specific
  compound shape if one is ever found, rather than guessing at handling now.
- [Risk] The D5 gate could mask a future InSv phrasing that doesn't literally contain "invulnerable
  save" (e.g. a glossary alias). → [Mitigation] Not observed anywhere in the current corpus scan; a
  future pattern addition to this family must re-verify its own trigger phrase is covered by the
  gate's substring, not assume it is.

## Migration Plan

1. Retype `CharacteristicEffect` (D1-D4); add the ranged/melee-restricted extraction to
   `RuleEffectClassifier`; add the D5 gate.
2. Build the standalone resolver and its tests, including the Shield-Dome ground-truth reproduction
   scenario.
3. Regenerate the checked-in baseline via `--write-baseline`; manually re-review only the reshaped
   InSv entries and any newly-surfaced split-save Effect results (same discipline as
   `widen-rule-effect-classification-coverage`).
4. Update the corpus report tool's default-only bucketing (D6).

No production/runtime migration — this change touches only `ProbHammer.Core` domain types, a dev-tool
report, and a checked-in baseline file; nothing deployed consumes an InSv Effect yet (see Non-Goals).

## Open Questions

- **Should `RuleEffectClassifier` become a composed pipeline of independently-gated "effect family"
  classifiers (each owning its own gate + extraction, composed by a thin orchestrator), rather than
  one growing static method?** Explicitly front-of-mind, not resolved here: only two families exist in
  practice after this change (Scalar, InvulnerableSave) — not enough real evidence to design the right
  composition shape without guessing, the same mistake this codebase has walked back before
  (`ComputeDerivedValue`'s rejected generic classifier parameter). Revisit once `KeywordEffect`/
  `AbilityEffect` are real, built things, giving three-plus real families to generalize from. Does not
  change this change's own specs, approach, or task breakdown — D5's gate is scoped narrowly enough to
  fit either outcome.
