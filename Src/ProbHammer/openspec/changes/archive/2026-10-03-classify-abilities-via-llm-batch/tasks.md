## 1. Stage 1 - Extractor tool

- [x] 1.1 Create `tools/AbilityPipeline/Extractor/` console app project, referencing `ProbHammer.Core`
- [x] 1.2 Factor `tools/RuleEffectClassificationReport/Program.cs`'s existing corpus walk (local +
      shared rules, Datasheet abilities, Enhancement abilities, Detachment rule text) into a form the
      new extractor can reuse, without modifying the existing report tool
- [x] 1.3 Compute a stable hash per normalized text (reuse `RuleEffectClassifier.Normalize`'s own
      normalization convention)
- [x] 1.4 Tag each record with its source kind (Datasheet ability / Enhancement / Detachment rule /
      Core rule / Army rule / raw shared rule)
- [x] 1.5 Merge against the previous run's output (if present) to populate/update
      `firstSeenAt`/`lastSeenAt` per hash, without discarding a hash that's now absent
- [x] 1.6 Write `tools/AbilityPipeline/data/ability-corpus.json` in a pretty-printed, human-searchable
      layout (not a single minified line)
- [x] 1.7 Run against the bundled `src/ProbHammer.Web/BsData/*.json` snapshot; confirm the distinct
      hash count lands close to the known ~3,830-text figure from the existing
      `classify-rule-effects-from-text` corpus run - investigate any large discrepancy before moving on
- [x] 1.8 Emit a summary report after each run: distinct record count per source kind, and the
      raw-occurrence-to-distinct-count ratio (which texts repeat most) - this is the concrete "regain
      a mental model of the corpus" deliverable, not just a byproduct of extraction

## 2. Stage 2 - Classifier tool scaffolding

- [x] 2.1 Create `tools/AbilityPipeline/Classifier/` console app project (references the Anthropic
      .NET SDK; does NOT reference `ProbHammer.Core`)
- [x] 2.2 Define the classifier's own standalone POCOs for the target schema: `Target`, `Effects`
      (Scalar / InvulnerableSave / Weapon-characteristic / FeelNoPain / WeaponKeywordGrant with an
      optional `replacesKeyword` / NamedAbilityGrant / NamedAbilityRemoval), per-effect
      `ResidualConditionBucket` + `ConditionText` (required for `evaluable-now`/`never`, never
      restating the top-level fields) + optional `ChoiceBranch` tag, top-level `ChoiceGroups`
      (min/max, option labels, optional `ConditionText` for a conditionally larger count),
      `Phases` (every activation phase), `TurnOwnership`/`UsageLimit`, and
      `CoverageStatus`/`UnclassifiedResidue` - revised 2026-09-28, see design.md's "Model and prompt
      review" section; `Phases` redefined and `KeywordTarget` made a list 2026-09-30 (tasks 4.0.1,
      4.0.7)
- [x] 2.3 Define the Batch API request/response plumbing: one request per un-cached hash, `custom_id`
      = hash, structured output (`strict: true` tool use) validated against the schema above -
      implemented via the Anthropic .NET SDK's native `OutputConfig`/`StructuredOutput.CreateJsonFormat<T>()`
      mechanism instead of forced tool-use (the SDK's current surface achieves the same
      strict-schema-validated-JSON goal through its newer, non-tool-based structured-output config;
      see `BatchRequestBuilder`'s own doc comment)
- [x] 2.4 Implement the incremental logic: reclassify when a hash is missing, when `promptVersion`
      differs from the current run's, or when `classifications.json`'s own stored text for a hash has
      drifted from the corpus's current text (a hand-editing consistency check, not a "text evolved"
      case - a hash is a content digest of its own text, so that case can't happen otherwise; see
      `IncrementalSelector`'s own doc comment, clarified 2026-09-26 after the ambiguity was noticed
      while explaining the pipeline to the user)

## 3. Prompt v1 and few-shot examples

- [x] 3.1 Write `tools/AbilityPipeline/prompts/v1/system-prompt.md` (classification instructions,
      referencing the current `fewshot/examples.json`)
- [x] 3.2 Write `tools/AbilityPipeline/prompts/v1/schema.json` (the structured-output schema matching
      task 2.2's POCOs) - generated directly from the `ClassificationResult` POCO tree via the new
      `Classifier schema` command (`StructuredOutput.ToJsonSchema<T>()`), so it can never drift from
      the code that actually submits it
- [x] 3.3 Curate `tools/AbilityPipeline/fewshot/examples.json` from the full extraction output (not
      solely the existing 41-entry `RuleClassificationBaseline`, per its own "too narrow" finding) -
      include at least one example per new Effect kind (Feel No Pain, weapon keyword grant,
      named-ability grant), at least one `"Once per battle"`-shaped usage-limited example (e.g.
      Finest Hour/Zealot-family text), at least one `evaluable-now` example (e.g. Adaptive Biology's
      wounds-based FNP upgrade), and at least one negative control (text that touches no
      characteristic at all) - all 9 examples (6 initial + 3 added after a dry-run found gaps: an
      InvulnerableSave grant, a plain unit-wide Scalar grant, and a `never`-bucket condition) pulled
      verbatim from real `ability-corpus.json` records and cross-checked against the corpus by hash
      (`Classifier check-fewshot`). Dry-run method: a subagent given prompt v1 + the few-shot set
      exactly as built, classifying 3 held-out real corpus texts, with its JSON output actually
      round-tripped through the real `ClassificationResult` deserializer (not just eyeballed) - all 3
      passed, and surfaced a real Stage 1 gap (see design.md's new "Parameterized template rules"
      Open Question) plus the two few-shot coverage gaps this task closed.
- [x] 3.4 Seed the named-ability-grant allowlist with the closed vocabulary already confirmed by
      corpus frequency (Fights First, Stealth, Lone Operative, Deep Strike, Infiltrators) -
      `tools/AbilityPipeline/prompts/v1/named-ability-allowlist.json`, each entry's real "has/have the
      X ability" grant occurrence count confirmed against the corpus (9-31 hits each) - superseded
      2026-09-30: the allowlist is retired in favour of collect-time resolution (task 4.0.5)

## 4. First corpus run

- [x] 4.0 Run a ~50-record random sample on Sonnet 5 under prompt v1: check the `complete` records in
      the `by-coverage` split, confirm `collect` reports non-zero cache reads, and fix any prompt or
      few-shot issues found before 4.1 - done 2026-09-30: 8/4/38 complete/partial/unclassifiable,
      caching confirmed, every record reviewed by the user; fixes are 4.0.1-4.0.8 (see design.md's
      "Sample review, 2026-09-30")
- [x] 4.0.1 Create prompt v2 (`prompts/v2/`): redefine `phases` as every phase the player needs to
      see the ability, with the worked examples from design.md, plus few-shot updates where an
      existing example's phases change under the new definition
- [x] 4.0.2 Add the core-Stratagem timing table to the v2 prompt (both names per renamed Stratagem)
- [x] 4.0.3 Add `Once per battle round` and `Twice per battle` to `UsageLimit`; regenerate
      `schema.json`
- [x] 4.0.4 Build the shared collect-time resolution step (verbatim + canonical form stored, an
      unresolved name demotes the record to `partial` with the name in its residue), and use it
      first for weapon keywords (`keyword`, `replacesKeyword`) against the corpus's weapon
      `KeywordsText` tokens, keeping the value
- [x] 4.0.5 Retire the named-ability allowlist: delete `named-ability-allowlist.json` and the
      prompt's allowlist sentence; resolve granted names through 4.0.4's step against `RuleGlossary`
      and BSData ability names - confirm `RuleGlossary.Normalize` handles a trailing `9"`
      (`Scouts 9"`)
- [x] 4.0.6 Re-run the same 50-record sample under v2 and compare against the v1 results before 4.1
      - done 2026-10-01, see design.md's "v2 sample re-run"
- [x] 4.0.7 Make `KeywordTarget` an all-of `keywords` list; prompt tells the model to split a
      faction-plus-type target into separate keywords (slash-OR stays residue); update the
      keyword-target few-shot examples
- [x] 4.0.8 Resolve `NamedWeapon` names (case- and singular/plural-tolerant) and target keywords
      through 4.0.4's step
- [x] 4.1 Submit a Batch API job against every hash in `ability-corpus.json` under prompt `v2`
- [x] 4.2 Poll for completion and write results into `tools/AbilityPipeline/data/classifications.json`,
      each record stamped with `hash`, `promptVersion`, `model`, `classifiedAt`, and a
      `reviewStatus` defaulting to pending - done 2026-10-03: 3,773 collected, 0 failed; with the
      50 sample records, 3,823 total: 752 complete / 455 partial / 2,616 unclassifiable
- [x] 4.3 Record actual token usage (printed by `collect`, including cache reads/writes) and cost,
      against the measured estimate: ~$67 uncached on Sonnet 5 at batch pricing, ~$15-25 if prompt
      caching hits - actual: input 413,451, output 1,931,056, cache read 63,677,255, cache write
      67,580 tokens; ~$16.50 at batch pricing

## 5. Review and acceptance gate

- [x] 5.1 Spot-check a random sample of `complete` records - these are used without review, so an
      error found here means a prompt or schema fix, not a one-off correction - done 2026-10-03:
      30 records, 27 correct / 2 wrong / 1 design gap; fixes go to the next prompt version (see
      design.md's "Full-run review, 2026-10-03")
- [x] 5.2 Group the `unclassifiedResidue` of `partial`/`unclassifiable` records into mechanic
      families, and decide which are out of scope for LivePlay vs. worth a new effect kind (see
      design.md's "Out-of-scope clauses" open question) - settled 2026-10-03 as a decision rule
      (in scope but not expressible vs. outside LivePlay's scope) plus an in-scope family backlog;
      the per-record split is left to the next prompt version rather than hand-clustered
- [x] 5.5 Normalize FNP qualifier sentinels (`"null"`, `""`, `none`, `None`, `all`, `__NONE__`) to
      `null` in `export` (stored records stay verbatim) and re-export the catalogue - done
      2026-10-03: all 66 unqualified FNP effects now export as `null`
- [x] 5.3 ~~Cross-check the new pipeline's output against all 41 entries in the existing
      `RuleClassificationBaseline` for the same underlying texts; record any disagreement and its
      resolution (schema difference vs. genuine error)~~ Superseded by `adopt-llm-ability-classifications`: the baseline was dropped without a
      cross-check (user decision, 2026-10-01)
- [x] 5.4 ~~Decide, based on 5.1-5.3, whether this pipeline is ready to inform a future
      "consume this output in LivePlay" change - not part of this change's own scope~~ Superseded: `adopt-llm-ability-classifications` adopts the output directly
      (user decision, 2026-10-01)

## 6. Documentation

- [x] 6.1 Update `.claude/vnext-ideas.md`'s pointer entry to reflect the standalone two-tool
      architecture actually built, once built
- [x] 6.2 Record the real corpus-run cost, distinct-hash count, and any schema corrections found
      during review in this change's own memory/design trail before archiving - design.md's
      "Full-run review, 2026-10-03"
