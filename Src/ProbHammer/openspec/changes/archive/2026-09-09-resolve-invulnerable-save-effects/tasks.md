## 1. Retype CharacteristicEffect

- [x] 1.1 In `Src/ProbHammer.Core/Domain/Catalogue/CharacteristicEffect.cs`, change
      `CharacteristicEffect` to an abstract record with `[JsonPolymorphic(TypeDiscriminatorPropertyName
      = "kind")]`, mirroring `RuleTarget.cs`'s exact convention.
- [x] 1.2 Add `ScalarCharacteristicEffect(string Characteristic, EffectVerb Verb, int Amount) :
      CharacteristicEffect` — today's shape, renamed, `[JsonDerivedType(typeof(...), "Scalar")]`.
- [x] 1.3 Add `InvulnerableSaveCharacteristicEffect(InvulnerableSave Value) : CharacteristicEffect` —
      no `Verb` field (see design.md D2), `[JsonDerivedType(typeof(...), "InvulnerableSave")]`.
- [x] 1.4 Update every existing `new CharacteristicEffect(...)` construction site (classifier, tests,
      any fixture) to `new ScalarCharacteristicEffect(...)`.

## 2. Widen RuleEffectClassifier

- [x] 2.1 Add the D5 pre-filter gate (`Contains("invulnerable save")`, normalized) as its own testable
      method/property ahead of the InSv pattern family.
- [x] 2.2 Add a ranged-restricted search pattern (anchored the same way `InvulnerableSaveGrant` is:
      `SentenceStart` + subject-shaped prefix) and a melee-restricted counterpart.
- [x] 2.3 Remove the `(?!\s+against\b)` exclusion from `InvulnerableSaveGrant`; reorder `ClassifyEffects`
      so the two restricted patterns are tried first, falling back to the uniform pattern only when
      neither restricted pattern matched (avoids double-extraction). Implementation note: the
      exclusion is *kept* on `InvulnerableSaveGrant` itself (not literally deleted) — it's still
      required so a non-melee/ranged restriction (e.g. Psychic Attacks) correctly extracts nothing
      once it reaches this fallback, per this requirement's own "scoped to the melee/ranged
      attack-type axis only" text and task 2.6's own Psychic-Attacks regression case; it no longer
      needs to distinguish ranged/melee specifically, since those are now caught earlier by the two
      new patterns and short-circuit this one before it runs.
- [x] 2.4 Combine whichever restricted pattern(s) matched into one `InvulnerableSave` (absent side ->
      `0`, per D3); emit one `InvulnerableSaveCharacteristicEffect`, adding its contributing match(es)
      to the existing `IsCaveated` position-tracking list.
- [x] 2.5 Confirm the uniform-grant scenario still emits `InvulnerableSaveCharacteristicEffect(new
      InvulnerableSave(N, N))` unchanged in observable behavior.
- [x] 2.6 Unit tests: uniform grant (Shield Dome), one-sided restricted (Ensorcelled Shield: ranged
      only), two-sided restricted (Veil of Medrengard), a non-melee/ranged restriction extracts nothing
      (Psychic Attacks — regression per design.md's Risks), a restricted grant with trailing
      unrecognized content is marked caveated (Ensorcelled Shield's Feel No Pain clause).

## 3. Standalone invulnerable-save effect resolver

- [x] 3.1 Add a new resolver (new file in `Domain/Catalogue`) taking an
      `InvulnerableSaveCharacteristicEffect` + source `Ability` + the characteristic's current
      `InvulnerableSaveCharacteristicView`, returning a resolved `InvulnerableSaveCharacteristicView`
      via `InvulnerableSaveCharacteristicView.Resolved(originalValue, effect.Value, [ability])`.
- [x] 3.2 Unit tests: uniform grant resolves to equal melee/ranged; restricted grant resolves with the
      absent side as no save; pre-mutation `OriginalValue` is preserved across resolution.
- [x] 3.3 Ground-truth test: resolving the Effect classified from Shield Dome's own Name+Text against
      Shield Dome's own `Ability` reproduces `ShieldDomeStatlineFlagRule.Apply`'s exact result.
- [x] 3.4 Confirm no call site wires this resolver into `AttachedUnitAggregator` or
      `StatlineFlagRuleCatalogue` (non-goal — leave `ShieldDomeStatlineFlagRule` untouched). Confirmed:
      `InvulnerableSaveEffectResolver` is referenced only from its own test file.

## 4. Baseline migration

- [x] 4.1 Regenerate `src/ProbHammer.Web/Data/RuleEffectClassifications.json` via the report tool's
      `--write-baseline`, picking up the mechanical `"kind"` discriminator on every existing entry.
      The old flat shape had no `"kind"` at all, so `RuleClassificationBaseline.Load` couldn't
      deserialize it against the now-polymorphic `CharacteristicEffect` base — migrated the file's
      `effects` arrays to the new discriminated shape via a `jq` transform first (every existing InSv
      entry was a uniform grant, so `amount` -> `{meleeInSv: amount, rangedInSv: amount}` is exact),
      then ran `--write-baseline`, which reported all 36 pre-existing entries unchanged, confirming
      the migration was correct.
- [x] 4.2 Manually re-review each reshaped InSv entry (`"kind": "InvulnerableSave"`) against its own
      source text, confirming `value.meleeInSv`/`value.rangedInSv` are correct. Every pre-existing
      tracked InSv entry is a uniform grant (no ranged/melee-restricted text was tracked before this
      change), so each reshapes to an equal melee/ranged pair matching its own stated value.
- [x] 4.3 Run the corpus report tool fresh; review every newly-surfaced split-save Effect result (the
      previously-Target-only/default-only texts now producing an `InvulnerableSaveCharacteristicEffect`)
      and add each as a new verified baseline entry, same discipline as
      `widen-rule-effect-classification-coverage`. Found and added 5: Ensorcelled Shield (ranged-only,
      caveated — trailing Feel No Pain grant), Veil of Medrengard (two-sided, not caveated), and three
      more real ranged-/melee-only grants (War Dog units' "*Invulnerable Save"/"Invulnerable Save
      (N+*)" footnoted profiles, and Judiciar's melee-only "*Invulnerable Save"). A fresh
      `--write-baseline` run afterward reports all 41 entries unchanged, 0 remaining Effect results,
      0 caveated entries needing review.
- [x] 4.4 Confirm existing non-InSv baseline entries show no semantic drift beyond the added `"kind"`
      field (spot-check via the report tool's "changed since verified" listing). Confirmed: 41
      tracked, 41 unchanged, 0 changed since verified.

## 5. Corpus report tool

- [x] 5.1 In `tools/RuleEffectClassificationReport/`, apply the D5 gate to the default-only bucket:
      exclude a gate-rejected text entirely; keep a gate-passed-but-unmatched text in its own
      distinct, reviewable listing.
- [x] 5.2 Run the full report against the live BSData clone; confirm the gate-passed-but-unmatched
      listing is small enough to fully review (not just spot-checked), and review it. Result: 47
      entries (out of 3,174 total default-only, 3,127 excluded by the gate) — fully reviewed. 46 are
      correctly, deliberately unmatched (a conditional preamble before the grant — "While this model
      is leading a unit...", "If it does, until the end of the phase..."; a restriction axis other
      than melee/ranged — Psychic Attacks, "against that attack"; or a pre-existing, unrelated
      markup-prefixed-subject/mid-sentence-comma-list limitation `InvulnerableSaveGrant` already had
      before this change). One real, confirmed gap found: T'au's "Skirmish Fighters" ("**^^Kroot^^**
      models... have a 6+ invulnerable save against melee attacks and a 5+ invulnerable save against
      ranged attacks.") — a two-sided grant whose first clause has a markup-prefixed subject (the
      pre-existing gap) and whose second clause joins via "attacks and a" with no comma (this
      change's own patterns require literal ", and"). Recorded in `.claude/vnext-ideas.md` as a
      tracked follow-up per design.md's own "don't guess at handling now" Risk mitigation — not fixed
      speculatively in this change.

## 6. Docs

- [x] 6.1 Update `.claude/domain-model-11e.md`'s "Rule Effect Classification (Text-Only)" section:
      `CharacteristicEffect`'s new polymorphic shape, the widened InSv extraction, the D5 gate, and the
      new `invulnerable-save-effect-resolution` capability.
- [x] 6.2 Add a `.claude/vnext-ideas.md` entry (or update the existing rule-effect-classification one)
      recording, prominently: `KeywordEffect`/`AbilityEffect` as a real, evidenced, deferred follow-on,
      and the open "should `RuleEffectClassifier` become a composed family-pipeline" question from
      design.md's Open Questions.
- [x] 6.3 Update `PROGRESS.md` with a summary of this change once implemented and verified.
