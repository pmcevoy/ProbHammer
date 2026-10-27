# Proposal

## Why

The 2026 Space Marine and Custodes codexes are now in BSData (f2325cc, 732a4ee, 2082e82). That
refresh left 484 live ability texts unclassified and made 529 old classifications stale. The v2
output also carries known prompt errors, recorded in the archived `classify-abilities-via-llm-batch`
design.md under "Full-run review, 2026-10-03". The worst-covered texts (Army and Detachment rules, 1
of 25 and 24 of 332 `complete`) fail mostly on roll modifiers, which the schema can't hold. We were
waiting for this codex before re-running, so now is the time to fix the prompt, add roll modifiers,
and reclassify everything once.

## What Changes

- **Prompt v3**, building on the existing draft (BS/WS weapon effects, the escaping fix), adds these
  rules:
  - Event triggers ("each time this unit Advances") are condition bucket `never`.
  - Models with the bearer's own keyword in that unit are target `Self`.
  - An FNP qualifier is only a damage source.
  - `turnOwnership` is set only when the text says whose turn.
  - Multi-word weapon names aren't split on "and".
  - Faction and type keywords are split.
  - Missing values are JSON `null`, never the string `"null"`.
- **Residue scope**: the model records residue as either *in scope but not expressible* or *outside
  LivePlay's scope*. Only in-scope residue keeps a record from `complete`.
- **New `RollModifier` effect kind**: data capture only, with no LivePlay consumer in this change.
  Fields:
  - `roll`: Hit, Wound, Damage, Save, Advance, Charge or BattleShock.
  - `rolledBy`: `friendly` or `enemy`.
  - `modifier`: add, subtract, reroll, rerollOnes, rerollSingle, criticalOn or ignoreModifiers.
  - `amount` and an optional weapon `selector`.
  - It also covers Damage-characteristic reduction against this unit.
- **Model choice by comparison**:
  - The classifier gains a model option, and a way to collect a test run into a separate file
    without touching `classifications.json`.
  - The v3-draft test set runs on Sonnet 5 and Sonnet 5.5, and the user picks the model for the
    full run.
- **Full v3 re-run** of every live text, about 3,790 requests. It is submitted only after the user's
  explicit go-ahead on the request count and cost estimate.
- **`export` drops stale classifications**: it writes only classifications whose text is in the live
  corpus and prints how many it dropped. `classifications.json` keeps every record.
- **Control-character check before `export`**: v2 wrote junk characters into 3 records, so `export`
  refuses to write if any classification string contains a control character.
- **Attack-type-only invulnerable-save footnotes resolve structurally**: a caveated InSv whose linked
  ability text is exactly "Against melee attacks only" or "Against ranged attacks only" resolves to
  that side, using the footnoted value, with no classification needed. This covers Judiciar's
  reworded footnote.
- **New rules checked in the app**: Combat Doctrines, Templar Vows, Aegis of the Emperor and
  Daughters of the Abyss are classified and checked live. Combat Doctrines stays a per-unit choice;
  the army-wide choice remains parked as "Army Rule profiles" in vnext-ideas.

Not in this change:
- LivePlay's phase reminder for roll modifiers (its own follow-up change).
- Per-effect timing, structured evaluable-now conditions, and a fixed FNP qualifier vocabulary.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ability-classification-pipeline`:
  - Roll-modifier recognition.
  - Residue scope, and what `complete` means.
  - The v3 classification rules for event triggers, `Self`, the FNP qualifier, turn ownership,
    weapon names and keyword splitting.
  - Choosing the model per run.
  - Collecting a test run to a separate file.
- `ability-classification-catalogue`:
  - Records carry an eighth effect kind (`RollModifier`) and out-of-scope residue.
  - `export` drops records whose text isn't in the live corpus.
  - `export` refuses control characters.
- `invulnerable-save`: an attack-type-only footnote resolves structurally, before the catalogue
  lookup.

## Impact

- **`tools/AbilityPipeline/Classifier`**:
  - `ClassificationEffect`/`ClassificationResult` models (`RollModifier`, the residue split).
  - `Program.cs` (`--model`, collecting to a separate file).
  - `CatalogueExporter` (drop stale records, control-character check).
  - `prompts/v3/` (prompt and regenerated schema).
  - `fewshot/examples.json` (roll-modifier examples).
- **`tools/AbilityPipeline/data`**: regenerated `ability-corpus.json` and `vocabulary.json` (the
  Extractor restamps every `LastSeenAt`), the re-run `classifications.json`, and `by-coverage/`.
- **`src/ProbHammer.Core`**:
  - `RuleEffect` gains a data-only `RollModifierEffect`.
  - `AbilityClassification` gains `OutOfScopeResidue`.
  - `InvulnerableSaveEffectResolver` gains the structural footnote rule.
- **`src/ProbHammer.Web`**: a regenerated `Data/ability-classifications.json`. The provenance
  popover keeps showing residue notes.
- **Tests**: unskip and update `ImportFlowTests.CustodianWardens_...` (Aegis of the Emperor FNP
  5+* against mortal wounds) and `CharacteristicEffectRealCorpusTests.Judiciar_...`, plus new tests
  for the changes above.
- **Cost**: two small draft runs (cents) plus one full Batch run of about $16–18.
