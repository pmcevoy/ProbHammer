## Why

`classify-rule-effects-from-text` shipped `RuleEffectClassifier` and a corpus report tool
(`tools/RuleEffectClassificationReport`) that classifies every distinct rule/ability text in the
live BSData clone and prints the results for manual review. A live-review pass already
hand-verified all 20 current Effect results as correct. Every further widening of the classifier
(new phrasing, new characteristics, eventually new effect verbs) re-runs that same report — and
today, the report has no memory: it reprints all 20 already-verified results alongside anything
genuinely new, forcing a full manual re-read every time, with no way to tell "this is unchanged
from what I already confirmed" from "this is new" without re-reading the text itself. That
undermines the exact discipline ([[feedback_precision_over_recall_for_text_classifiers]]) this
classifier depends on — a reviewer who has to re-read everything every time will eventually skim,
and a skimmed review is not a verified one.

`RuleClassification` isn't wired to anything yet, but it's expected to eventually gate what
`/LivePlay` is allowed to apply to a real game — the same "never guess, fail closed" posture
already used for `IsGameModeGated`/`IsTier1OrTier2` elsewhere in this codebase. A durable,
checked-in record of which specific classifications a human has actually verified is the natural
mechanism for that gate, not just a review-tool convenience — which is why it belongs alongside
the web app's own bundled data, not buried in a dev-only tool project.

## What Changes

- A checked-in JSON baseline (`src/ProbHammer.Web/Data/RuleEffectClassifications.json`) records,
  per distinct normalized rule/ability text, the `RuleClassification` a human has explicitly
  verified as correct.
- `tools/RuleEffectClassificationReport` diffs each corpus text's freshly-computed classification
  against the baseline, per field. This changes behavior only for a text already recorded in the
  baseline: **unchanged** (every already-baselined field's value still matches) collapses to a
  single summary count and is never reprinted, while **drift** (a field the baseline already
  recorded now computes a different value) is always resurfaced — that's the regression signal the
  whole mechanism exists to catch. A field newly added to `RuleClassification`'s own schema since a
  baseline entry was written is schema growth, not drift: it silently backfills into the baseline if
  it computes to that field's defined default, and surfaces once, labeled as new information (not as
  a request to re-verify fields that didn't change), otherwise. A text with no baseline entry at all
  — every Target-only and default-only result today, and any Effect result not yet reviewed — is
  unaffected: it keeps appearing every run under the report's existing Effect/Target-only/
  default-only sections, exactly as it does today. The baseline only ever suppresses what it has
  actually recorded; nothing is hidden by default.
- The report tool gains a `--write-baseline` mode: after a review pass, it snapshots the current
  full classification of every corpus text already in (or newly added to) the baseline, so
  accepting a review is one command and the resulting `git diff` is a readable audit trail of
  exactly what was just accepted.
- `src/ProbHammer.Web/Data/` is a new folder, a sibling of `src/ProbHammer.Web/BsData/` — never
  nested inside it, since `LocalDiskBsdataCatalogueSource.ListFileNames()` treats every `*.json`
  file directly under the configured `Bsdata:RootDirectory` as a BSData catalogue file to parse.
  Bundled into the Docker image the same way `BsData/` is: excluded from the project's implicit
  content globbing (`<Content Remove>`), copied in via its own `COPY --link` layer in the
  Dockerfile.
- The baseline file is seeded with the 20 already-manually-verified Effect results from the
  current corpus run as this change's own initial content. A baseline entry can carry an optional
  human-authored note recording that its classification, while verified correct as far as it goes,
  is known to omit real content its text states (e.g. Blastajet Force Field's InSv grant is
  correctly extracted, but its text also states losing a keyword the classifier has no vocabulary
  for yet) — 5 of the 20 seed entries (Blastajet Force Field, Leader-beast, Lesk's Heroes,
  Redoubtable Machine Spirit, Scattershield) carry one. `--write-baseline` refreshes only computed
  classification fields and never touches this note.
- Out of scope for this change: consuming `RuleClassification`/the baseline from `/LivePlay` or any
  `BsdataDatasheetMapper`/`AttachedUnitAggregator` call site (still "not wired to anything," per
  `.claude/vnext-ideas.md`); the possessive-characteristic-phrasing fix, the "+1 OC" shorthand fix,
  and the caveated-classification work that motivated this change all build on top of it rather than
  landing in it.

## Capabilities

### New Capabilities
- `rule-effect-classification-baseline`: records which specific rule/ability text classifications
  have been human-verified, and drives the corpus report tool's new/drift/unchanged/backfill
  distinction so a reviewer never has to re-read an already-verified, unchanged result.

### Modified Capabilities
- `rule-effect-classification`: the existing "Corpus-Wide Classification Reporting" requirement
  gains a dependency on the new baseline capability for its new/drift/unchanged bucketing; no
  change to `RuleEffectClassifier`'s own classification behavior.

## Impact

- New file: `src/ProbHammer.Web/Data/RuleEffectClassifications.json` (checked in, Docker-bundled).
- `src/ProbHammer.Web/ProbHammer.Web.csproj`: new `<Content Remove="Data/**" />` (mirroring
  `BsData/**`).
- `src/ProbHammer.Web/Dockerfile`: new `COPY --link src/ProbHammer.Web/Data/ /app/Data/` layer.
- `tools/RuleEffectClassificationReport/Program.cs`: loads the baseline, computes the per-field
  diff, and for a text already in the baseline substitutes a drift/backfill listing (or a collapsed
  unchanged count) for what would otherwise be its normal Effect/Target-only/default-only entry.
  Every text not yet in the baseline keeps appearing under those existing sections unchanged. Adds
  `--write-baseline`.
- No change to `src/ProbHammer.Core/Domain/Catalogue/RuleEffectClassifier.cs` or
  `RuleClassification`'s own shape in this change.
