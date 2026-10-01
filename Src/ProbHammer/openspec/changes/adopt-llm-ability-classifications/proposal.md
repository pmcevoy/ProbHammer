## Why

`/LivePlay`'s ability effects (flagged Statline values, mutated weapon profiles, Attacks
contributions, resolved invulnerable-save caveats, Detachment-rule keyword targets) are driven by a
41-entry, hand-verified `RuleClassificationBaseline` produced by the regex `RuleEffectClassifier`.
`classify-abilities-via-llm-batch` now classifies the whole ~3,800-text corpus into a richer schema
(per-effect conditions, phases, turn ownership, usage limits, choice groups, coverage status, and
four new effect kinds), so the regex classifier and its baseline are superseded. This change swaps
the runtime onto the new classifications, mimicking today's `/LivePlay` behavior, and deletes the
legacy layer outright rather than running the two side by side.

## What Changes

- **New runtime ability-classification catalogue** in `ProbHammer.Core`: a checked-in file loaded
  once, looked up by the content hash of an ability's normalized text, modeling the full v2 schema -
  target (keyword targets become an all-of keyword list), classified effects with their condition
  bucket/condition text/choice branch, choice groups, phases, turn ownership, usage limit, coverage
  status and residue. All seven effect kinds deserialize, including `FeelNoPain`,
  `WeaponKeywordGrant`, `NamedAbilityGrant` and `NamedAbilityRemoval`, which nothing consumes yet.
- **New `export` command in the pipeline's Classifier tool** that writes the catalogue file into
  `src/ProbHammer.Web/Data/`, substituting each resolved name's canonical BSData spelling and
  dropping review metadata.
- **One "unconditional effect" rule** replaces both of today's caveat rules: an effect applies only
  when it has no residual condition, no usage limit, no choice branch, and no turn ownership.
  Coverage status does not gate anything - a `partial` or `unclassifiable` record is used for
  whatever it does capture.
- **Every existing consumer is re-pointed** at the catalogue with today's behavior preserved:
  Statline flagging, weapon S/AP/D mutation and Attacks contributions, the weapon
  unresolved-ability reference (now driven per effect, not per record), caveated
  invulnerable-save resolution, and Detachment-rule keyword-target attachment.
- **BREAKING (internal): legacy classification removed** - `RuleEffectClassifier`,
  `RuleClassification`, `RuleClassificationBaseline`, `RuleClassificationDiff`,
  `src/ProbHammer.Web/Data/RuleEffectClassifications.json`, `tools/RuleEffectClassificationReport/`,
  `CharacteristicModifierCandidate` (its only consumer was the report tool), their tests, and their
  `.claude/` documentation. The normalization the content hash depends on moves into the new
  catalogue first, unchanged.
- Until the full-corpus run is collected, the catalogue holds only the 50-record sample, so the
  abilities the baseline used to resolve (Vexilla, Shield Dome, ...) temporarily resolve nothing.
  Accepted.

## Capabilities

### New Capabilities
- `ability-classification-catalogue`: the runtime catalogue - its file, hash-keyed lookup, schema
  shape, the export that produces it, and the unconditional-effect rule every consumer shares.

### Modified Capabilities
- `statline-flag-rules`: driven by the catalogue instead of the baseline; only unconditional effects
  flag a value.
- `attached-unit-tracker`: Aggregate Weapon Count View's applied-vs-unresolved split becomes per
  effect (unconditional vs. conditional) instead of per baseline entry (caveated or not).
- `invulnerable-save`: a caveated invulnerable save resolves only from an unconditional catalogue
  effect.
- `army-roster-enrichment`: Detachment-rule keyword targets come from the catalogue and match a
  unit carrying every listed keyword.
- `catalogue-json-ingestion`: characteristic-modifier classification removed; `BsModifier` data is
  read for hidden-gating only.
- `datasheet-catalogue`: on-demand characteristic-modifier candidate exposure removed.
- `rule-effect-classification`: removed entirely.
- `rule-effect-classification-baseline`: removed entirely.

## Impact

- `ProbHammer.Core`: new catalogue types and loader; `AttachedUnitAggregator`,
  `DetachmentRuleInboundAbilityResolver`, `BsdataDatasheetMapper`, `Datasheet` re-pointed or
  trimmed; legacy classification types deleted; `CharacteristicEffect`/`RuleTarget` reshaped.
- `ProbHammer.Web`: `Program.cs` registration, `LivePlay.cshtml.cs`, `LivePlayCasualtyService`,
  `ArmyRosterProvider`; `Data/RuleEffectClassifications.json` replaced by
  `Data/ability-classifications.json`.
- `tools/`: `RuleEffectClassificationReport/` deleted; `AbilityPipeline/Extractor` hashes through the
  relocated Core normalization; `AbilityPipeline/Classifier` gains `export`.
- Tests: baseline fixtures and every baseline-driven test ported to catalogue fixtures; regex
  classifier, baseline, diff and characteristic-modifier tests deleted.
- Docs: `.claude/domain-model/rule-effect-classification.md` and `characteristic-modifier-caveats.md`
  deleted; a new catalogue topic file; indexes and dependent topic files updated.
- `classify-abilities-via-llm-batch`: tasks 5.3/5.4 and its "old tool untouched" non-goal are
  superseded by this change and need amending there.
