## Why

`RuleEffectClassifier` currently discards every attack-type-restricted invulnerable-save grant
("...against ranged attacks") to zero Effects, and even its unrestricted "Set InSv N" extractions
can never be resolved into a real Statline value — `CharacteristicModificationResolver`/
`CharacteristicModificationKinds` explicitly excludes InSv, since it's a compound melee/ranged
value, not a plain scalar. `ShieldDomeStatlineFlagRule` remains the only thing that actually
produces a real `InvulnerableSaveCharacteristicView` today, hand-authored rather than driven by the
classifier — even though many of the classifier's own 36 human-verified baseline entries are
exactly this shape.

## What Changes

- `CharacteristicEffect` becomes abstract with two sealed subtypes: `ScalarCharacteristicEffect`
  (today's `(Characteristic, Verb, Amount)` shape, renamed) and a new
  `InvulnerableSaveCharacteristicEffect(InvulnerableSave Value)`. **BREAKING**: changes
  `CharacteristicEffect`'s public shape and its JSON serialization (gains a `"kind"` discriminator,
  mirroring `RuleTarget`'s existing `[JsonPolymorphic]`/`[JsonDerivedType]` convention). `Effects`
  itself stays exactly the same list/field on `RuleClassification` — InSv never leaves it.
- `RuleEffectClassifier` recognizes attack-type-restricted invulnerable-save grants — both one-sided
  (e.g. Ensorcelled Shield: ranged only, melee absent) and two-sided (e.g. Veil of Medrengard: both
  stated, different values) — extracting an `InvulnerableSaveCharacteristicEffect` instead of
  excluding them. The current `(?!\s+against\b)` exclusion in `InvulnerableSaveGrant` is removed and
  replaced with real extraction, scoped to the melee/ranged axis only (see Non-Goals).
- A cheap, provably-safe pre-filter gate (requires the literal substring "invulnerable save",
  case/whitespace-normalized) runs ahead of the invulnerable-save pattern family specifically. The
  corpus report tool distinguishes a confidently-rejected text (never plausible) from one that
  passed the gate but matched no pattern (the small, actually-reviewable set).
- A new standalone resolver turns a classified `InvulnerableSaveCharacteristicEffect` plus its
  source `Ability` into a real `InvulnerableSaveCharacteristicView` — mirrors
  `characteristic-modification-kind`'s own provably-correct-in-isolation, unwired discipline. Not
  consumed by `AttachedUnitAggregator`/`StatlineFlagRuleCatalogue` in this change;
  `ShieldDomeStatlineFlagRule` is untouched.
- `RuleEffectClassifications.json`'s existing InSv-carrying entries migrate to the new discriminated
  shape (small set, manual re-review); every other entry picks up the new `"kind"` discriminator
  mechanically via `--write-baseline`, needing no re-review.

### Non-Goals (named for a later change, not built here)

- `KeywordEffect`/`AbilityEffect` sibling types — real and evidenced (5 of the 12 existing baseline
  entries with a known-gap note specifically name missing keyword/ability content), same footing as
  the already-deferred `WeaponEffect`.
- Whether `RuleEffectClassifier` itself should become a composed pipeline of independently-gated
  family classifiers, rather than one growing static method — revisit once Keyword/Ability effects
  are real, not designed from one data point (InSv) now.
- The Psychic-Attack/Daemon-attack-source restriction axis (confirmed real in the corpus, e.g.
  "invulnerable save against Psychic Attacks") — `/LivePlay` has no way to represent a
  qualifier-restricted save at all; if ever addressed, it resolves as a Caveated InSv value
  referencing the source ability, not a new `InvulnerableSave` shape.

## Capabilities

### New Capabilities

- `invulnerable-save-effect-resolution`: resolves a classified `InvulnerableSaveCharacteristicEffect`
  plus its source `Ability` into a real `InvulnerableSaveCharacteristicView` — standalone, unwired,
  mirroring `characteristic-modification-kind`.

### Modified Capabilities

- `rule-effect-classification`: the Unconditional Characteristic Effect Extraction requirement
  widens to cover attack-type-restricted invulnerable-save grants (one- and two-sided) instead of
  excluding them; the Caveated Signal requirement gains coverage for an invulnerable-save-only
  extraction with real remaining content; Corpus-Wide Classification Reporting gains the
  rejected-vs-no-match distinction for this pattern family.

## Impact

- `Src/ProbHammer.Core/Domain/Catalogue/CharacteristicEffect.cs` (abstract base + two sealed
  subtypes, replacing today's single sealed record)
- `Src/ProbHammer.Core/Domain/Catalogue/RuleEffectClassifier.cs` (new patterns + gate)
- New file: an invulnerable-save effect resolver in `Domain/Catalogue`
- `src/ProbHammer.Web/Data/RuleEffectClassifications.json` (baseline migration)
- `tools/RuleEffectClassificationReport/` (reject-vs-no-match reporting split)
- Tests: `RuleEffectClassifierTests`, baseline-loading/diff tests, new resolver tests
