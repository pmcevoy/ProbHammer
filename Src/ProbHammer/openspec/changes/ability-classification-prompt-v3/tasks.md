# Tasks

## 1. Refresh the corpus

- [x] 1.1 Back up `tools/AbilityPipeline/data/ability-corpus.json` and `vocabulary.json` to the scratchpad, run the Extractor, and diff the corpus against the backup: only `LastSeenAt` restamps plus about 484 added and 529 dropped hashes (the 2026-10-26 measurement). Record the counts in design.md.
- [x] 1.2 Find the 2026 corpus hashes for Combat Doctrines, Templar Vows, Aegis of the Emperor, Daughters of the Abyss and Judiciar's footnote. Check which of the 20 `ReviewerNote` records are still live. Write both lists into design.md's draft test set.

## 2. Classifier schema: roll modifiers and the residue split

- [x] 2.1 Add a `RollModifierEffect` (roll, rolledBy, modifier, amount, optional selector) and its enums to `Classifier/Models`. Add an optional `OutOfScopeResidue` to `ClassificationResult`. Verify that `schema` regenerates `prompts/v3/schema.json` with the new kind and field, and that `prompts/v2/schema.json` is unchanged.
- [x] 2.2 Map `RollModifier` names in `NameResolver` (named-weapon selector) and `CatalogueExporter.Canonicalize`. Verify with a classifier test that a roll modifier's named weapon is exported canonically. Classifier tests go in a new `tools/AbilityPipeline/Classifier.Tests` project in the solution (user decision, 2026-10-26).
- [x] 2.3 Add `RollModifierEffect` (data-only) to Core's `RuleEffect` and `OutOfScopeResidue` to `AbilityClassification`. Verify with catalogue tests that a roll-modifier record loads with every field and that a ninth unknown kind still fails the load.
- [x] 2.4 Show out-of-scope residue after the in-scope note in `ValueProvenance` provenance lines. Verify with a provenance test that both notes render, in order.
- [x] 2.5 Update `.claude/domain-model/ability-classification-catalogue.md` (eight effect kinds, the residue split).

## 3. Classifier commands

- [x] 3.1 Add `--model sonnet-5|sonnet-5-5` to `submit` and store it in `pending-batch.json`. Upgrade the Anthropic SDK if a version exposes a Sonnet 5.5 constant; otherwise build the model from `claude-sonnet-5-5`. Verify with `preview <hash> --model sonnet-5-5` that the request names Sonnet 5.5, without submitting.
- [x] 3.2 Add `collect --out <file>`, which writes results and usage to that file and leaves `classifications.json` untouched. Verify with a test that `classifications.json` is byte-identical after collecting to a named file.
- [x] 3.3 Make `export` leave out hashes that aren't live (`LastSeenAt` older than the corpus's newest) and print the count, and make `IncrementalSelector` skip them too. Verify with a `CatalogueExporter` test that a stale record is left out, the count is reported, and the classifications are unchanged, and with a selector test that a stale hash isn't selected.
- [x] 3.4 Make `export` refuse control characters other than a line feed, reporting the hash and field. Verify with a test that uses a `\r` in condition text.
- [x] 3.5 Update the Usage line and the pipeline notes in `.claude/domain-model/ability-classification-catalogue.md` for `--model`, `--out` and dropping stale records at export.

## 4. Prompt v3

- [x] 4.1 Add the `RollModifier` section to `prompts/v3/system-prompt.md`, covering:
  - the add/subtract verbs, `rolledBy`, and the re-roll variants;
  - criticalOn, ignoreModifiers, and Damage reduction as `Damage`/`enemy`/`subtract`;
  - the selector for attack rolls only, and one effect per roll;
  - enemy-wide debuffs stay residue.

  Also remove "re-rolls, modifiers to Hit/Wound/Advance/Charge rolls, ignoring modifiers" from the residue list. Verify `preview` shows the new section.
- [x] 4.2 Add the residue-scope rule (in-scope vs out-of-scope lists, and what `complete` means) to the prompt.
- [x] 4.3 Add the classification rules to the prompt, each with a worked example from the spec:
  - event triggers are `never`;
  - the bearer's own models are `Self`;
  - an FNP qualifier is a damage source only;
  - turn ownership only when stated;
  - weapon names with "and" stay whole;
  - faction and type keywords are split;
  - JSON `null`, never `"null"`.
- [x] 4.4 Add two `RollModifier` few-shot examples (a friendly ranged re-roll and an enemy melee Hit modifier) from live hashes, chosen with a fixed seed. Verify `check-fewshot` passes and the v2 examples still load. Also rewrite the five examples v3 reverses (see design.md).

## 5. Structural footnote resolution

- [x] 5.1 In the InSv caveat resolution, resolve an exact "Against melee/ranged attacks only" linked-ability text (ignoring case, whitespace and a trailing full stop) from the footnoted value, before the catalogue lookup. Verify with resolver tests for bare melee, ranged with a full stop, and longer text falling through.
- [x] 5.2 Unskip `CharacteristicEffectRealCorpusTests.Judiciar_MeleeOnlyInvulnerableSaveFootnote_ResolvesViaItsClassification` and rename it to say "structurally", not "via its classification". Verify it passes against the bundled BSData.
- [x] 5.3 Update `.claude/domain-model/invulnerable-save-effect-resolution.md` with the structural rule.

## 6. Draft runs and model choice

- [x] 6.1 Back up `classifications.json`. State the request count and cost estimate for two draft runs (test set from 1.2 plus about 10 seeded roll-modifier texts, on Sonnet 5 and Sonnet 5.5), and get the user's go-ahead.
- [x] 6.2 Submit and collect each run with `--prompt v3 --label v3-draft --model <m>`, collecting to `data/v3-draft-sonnet-5.json` and `data/v3-draft-sonnet-5-5.json`. Verify `classifications.json` is unchanged.
- [x] 6.3 Compare the two runs in the user's spot-check format, one record first and then 5 at a time, with both models' fields side by side. Report:
  - which `ReviewerNote` fixes each model reproduces unaided;
  - roll-modifier correctness;
  - output tokens per record.

  Keep verdicts in a scratchpad file.
- [x] 6.4 Record the user's model choice and any prompt fixes in design.md. If the prompt changes, re-run only the affected draft records, after another go-ahead.

## 7. Full run

- [x] 7.1 Set the chosen model as the default. State the full-run request count (`submit` dry count against `v3`) and cost estimate, and get the user's explicit go-ahead before `submit --prompt v3`.
- [x] 7.2 After the batch ends, back up `classifications.json`, then `collect`. Record succeeded/failed counts, token usage and cost, and the complete/partial/unclassifiable split in design.md.
- [x] 7.3 Diff the 20 hand-corrected records against the backup. Re-apply by hand any fix v3 didn't reproduce, keeping its `ReviewerNote`, and list them in design.md.
- [x] 7.4 Spot-check in the user's format: a fixed-seed sample of `complete` records (including some `complete` only because of out-of-scope residue), roll-modifier records, and the four new Army/Detachment rules. Record the verdicts in design.md.

## 8. Export and verification

- [x] 8.1 Run `resolve`, then `export`. Verify that the control-character check passes (fix any flagged record first) and that the reported stale count is about 529.
- [x] 8.2 Unskip `ImportFlowTests.CustodianWardens_ShowAConditionalFeelNoPainTile_AndNoValuelessFeelNoPainEntry` and update its assertions for Aegis of the Emperor's FNP 5+* against mortal wounds. Run the full test suite and confirm it is green, with no remaining skips tied to this run.
- [x] 8.3 Check the app live against `data/nr-custodes-for-unit-tests.json`:
  - Vigilator Squad's and Custodian Wardens' FNP tiles are amber "–" via the footnote rule;
  - Living Fortress is switchable;
  - Aegis of the Emperor and Daughters of the Abyss render.

  Verify the DOM state with chrome-devtools, not just screenshots.
- [x] 8.4 Check the app live against `data/nr-templars-for-unit-tests.json`:
  - Combat Doctrines offers its choice per unit;
  - Templar Vows renders under Vow-Sworn Crusaders;
  - Judiciar's InSv is resolved melee 4+.
- [x] 8.5 Remove the shipped v3 entry from `.claude/vnext-ideas.md`, keeping the parked items (per-effect timing, evaluable-now conditions, FNP qualifier vocabulary). Add the roll-modifier reminder strip as a new entry, then update `PROGRESS.md`.
