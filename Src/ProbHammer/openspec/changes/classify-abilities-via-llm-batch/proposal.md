## Why

Two things converged this exploration. First, a corpus review found `RuleEffectClassifier`'s flat
regex-pattern architecture hitting real limits (recurring matching bugs, each fixed with another
hand-tuned escape hatch) and a large, previously-unrecognized real gap it has no clean way to absorb:
**Feel No Pain grants** (153 corpus hits, zero representation anywhere in the codebase today).
Second, and more directly motivating: the corpus itself (46 BSData files, `catalogueLinks` chaining
across them, `infoLink`/`infoGroup`/`sharedRules` all as different ways the same rule text surfaces)
has grown too large and structurally complex to hold a reliable mental model of by browsing files
directly - confirmed the hard way mid-exploration, when a specific ability ("Dark Pact") was cited
confidently from an old memory-file paraphrase and turned out, on checking the real corpus text, to
be flatly wrong.

`RuleEffectClassifier.Classify` is already offline-only (never called from live runtime code - see
`AttachedUnitAggregator.cs`'s own comment), so the classification mechanism can be replaced, or run
alongside a new one, without touching any shipped runtime path. An initial cost estimate (Claude Haiku
4.5 via the Anthropic Batch API, against the actual known corpus size of ~3,830 distinct texts) put
a full one-time classification run at roughly $4-5. Real test runs (2026-09-28) superseded that:
~$24 on Haiku 4.5 or ~$67 on Sonnet 5 uncached at batch pricing, ~$15-25 on Sonnet 5 with prompt
caching - Sonnet 5 chosen for accuracy (see design.md). Every subsequent BSData-update re-run only
classifies new or changed texts. Still cheap enough to justify building real, standalone tooling
around this rather than another round of hand-tuned regex.

## What Changes

- **New, standalone Stage 1 tool** ("extractor"): walks the entire BSData corpus once, reusing the
  existing, already-hardened `Domain.Catalogue.Bsdata` closure-resolution/rule-walking machinery
  (no new corpus-parsing logic), and produces one flat, human-readable, hash-keyed JSON file -
  `ability-corpus.json` - covering every distinct rule/ability/Enhancement/Detachment-rule text in
  the corpus. This file becomes the new default place to answer "what does the corpus actually
  contain" - checkable directly (vim/grep) instead of relying on memory or ad hoc `jq` dumps.
- **New, standalone Stage 2 tool** ("classifier"): reads `ability-corpus.json`, submits exactly one
  Anthropic Batch API request per hash not already classified under the current prompt version, and
  writes/updates `classifications.json` - a checked-in classification result per hash, each with a
  coverage status marking whether it is safe to use without review. Prompt wording and the target output schema are versioned as checked-in files
  (`prompts/v<N>/system-prompt.md` + `schema.json`); few-shot examples live in one shared file
  (`fewshot/examples.json`), independent of prompt-wording iteration.
- **New classification vocabulary** captured by Stage 2's schema: `FeelNoPainCharacteristicEffect`,
  a weapon-scoped keyword-grant effect, and a named-ability-grant effect - plus
  (see design.md's explicit decision) `Phases`/`TurnOwnership` extracted as their own fields and a
  3-way `ResidualConditionBucket` (`none` / `evaluable-now` / `never`) per effect, superseding a
  plain `IsCaveated` bool for this pipeline's own output.
- **File-based storage throughout**, deliberately not a database - chosen for direct hand-editability
  ("surgery on individual records"), git-diffability, and grep/vim searchability. Nothing here
  precludes moving to something more robust later if the file-based approach stops scaling.
- **The existing `RuleEffectClassifier`, `tools/RuleEffectClassificationReport/`, and the shipped
  `RuleClassificationBaseline`/`RuleEffectClassifications.json` are entirely untouched** - this
  change is purely additive, new tooling running in parallel, not a replacement yet.
- **Explicitly out of scope**: any change to `ProbHammer.Core`/`ProbHammer.Web` runtime types,
  resolvers, or rendering. Wiring the new pipeline's output into LivePlay is a deliberately separate,
  later change, gated on this pipeline's output being trusted (see design.md's acceptance-gate
  question).

## Capabilities

### New Capabilities
- `ability-corpus-extraction`: the standalone Stage 1 tool's behavior contract - what it extracts,
  how it's keyed and tagged, and the guarantees its output file makes.
- `ability-classification-pipeline`: the standalone Stage 2 tool's behavior contract - the batch
  classification process, prompt/schema versioning, incremental re-classification, and
  coverage-driven review.

### Modified Capabilities
(none - `rule-effect-classification` and `rule-effect-classification-baseline` are untouched by this
change)

## Impact

- New tool projects (proposed layout, see design.md): `tools/AbilityPipeline/Extractor/`,
  `tools/AbilityPipeline/Classifier/`, plus `tools/AbilityPipeline/prompts/`,
  `tools/AbilityPipeline/fewshot/`, `tools/AbilityPipeline/data/`.
- No changes to `src/ProbHammer.Core`, `src/ProbHammer.Web`, or `tools/RuleEffectClassificationReport/`.
- No changes to any Docker image or deployed runtime behavior - the new pipeline's output files are
  not consumed by anything shipped yet.
