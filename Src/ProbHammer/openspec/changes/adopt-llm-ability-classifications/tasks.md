## 1. Hash key into Core (D1)

- [ ] 1.1 Add `AbilityTextKey` (`Domain/Catalogue/`) with `Normalize` moved verbatim from
      `RuleEffectClassifier` and `Hash` moved from the extractor's `ContentHash`; leave
      `RuleEffectClassifier.Normalize` delegating to it until group 5
- [ ] 1.2 Point the extractor's `ContentHash`/`AbilityCorpusAggregator` at `AbilityTextKey`; re-run
      the extractor and confirm `ability-corpus.json`'s hashes are unchanged (revert the
      `LastSeenAt` restamp if nothing else changed)

## 2. Core catalogue model (D2, D3, D5)

- [ ] 2.1 Move `GamePhase`/`GameTurn` to `Domain.Catalogue` (Rider move-to-namespace)
- [ ] 2.2 Rename `CharacteristicEffect` → `RuleEffect` (Rider rename); change the weapon effect's
      discriminator to `WeaponCharacteristic`; add data-only `FeelNoPainEffect`,
      `WeaponKeywordGrantEffect`, `NamedAbilityGrantEffect`, `NamedAbilityRemovalEffect`
- [ ] 2.3 Change `KeywordRuleTarget` to an all-of `Keywords` list and update its current callers
- [ ] 2.4 Add `AbilityClassification`, `ClassifiedEffect`, `ChoiceGroup`, `ChoiceBranch`,
      `ConditionBucket`, `UsageLimit`, `CoverageStatus`, with JSON names matching the v2 schema
- [ ] 2.5 Implement `IsUnconditional` and unit-test every Unconditional Effect Rule scenario
- [ ] 2.6 Add `AbilityClassificationCatalogue` (`Load`, `Empty`, `FromRecords`, `TryGet(text)`);
      test missing-file → empty, unknown effect kind → load fails, every effect kind loads,
      multi-keyword target round-trips, and lookup through a typographic apostrophe/NBSP

## 3. Export command (D4)

- [ ] 3.1 Add `Classifier export`: join `classifications.json` with `ability-corpus.json`, substitute
      canonical names from each record's `Resolution`, convert `InvulnerableSave` to Core's shape,
      drop review metadata/prompt version/model, write records sorted by hash to
      `src/ProbHammer.Web/Data/ability-classifications.json`
- [ ] 3.2 Run `resolve` then `export` against the current 50-record v2 sample; confirm a second
      export is byte-identical
- [ ] 3.3 Add a test loading the real checked-in file through Core's loader and re-hashing every
      record's `Text` against its key

## 4. Port consumers onto the catalogue (D5, D6, D7)

- [ ] 4.1 Register the catalogue singleton in `Program.cs` (`AbilityClassifications:FilePath`) and
      replace the baseline in `LivePlay.cshtml.cs`, `LivePlayCasualtyService`, `ArmyRosterProvider`
      (Rider change-signature)
- [ ] 4.2 `AttachedUnitAggregator`: `ResolveCaveatedInvulnerableSaves` and `ApplyStatlineFlagRules`
      use unconditional effects only; keep target scoping and the Detachment-rule origin exception
- [ ] 4.3 `AttachedUnitAggregator`: weapon S/AP/D and Attacks paths use unconditional effects;
      `FindUnresolvedAbilities` uses conditional effects; split per effect, not per record
- [ ] 4.4 `DetachmentRuleInboundAbilityResolver`: catalogue lookup, all-of keyword match, ignoring
      case
- [ ] 4.5 Replace `RuleClassificationBaselineFixtures` with catalogue fixtures (D9) and port every
      baseline-driven test in `Domain/Roster/`, `Domain/Import/` and `Web/`, including
      `WeaponCharacteristicEffectRealCorpusTests` and
      `UnifiedCharacteristicEffectResolutionRegressionTests` onto fixtures
- [ ] 4.6 Add tests for the new spec scenarios: conditional Statline effect not flagged, conditional
      InSv stays caveated, one record both applies and defers, partial record's effect applies,
      multi-keyword Detachment target

## 5. Delete the legacy layer (D8)

- [ ] 5.1 Delete `RuleEffectClassifier`, `RuleClassification`, `RuleClassificationBaseline`
      (+Entry/File), `RuleClassificationDiff`, and their tests
- [ ] 5.2 Delete `CharacteristicModifierCandidate`, `BsdataDatasheetMapper`'s candidate classifier,
      `Datasheet.CharacteristicModifierCandidates`, and their tests (incl. the corpus scan test)
- [ ] 5.3 Delete `tools/RuleEffectClassificationReport/` and remove it from the solution
- [ ] 5.4 Delete `src/ProbHammer.Web/Data/RuleEffectClassifications.json`
- [ ] 5.5 Grep `src/`, `tools/`, `tests/` for any remaining reference to the deleted names; full
      build and test run green

## 6. Documentation and specs

- [ ] 6.1 Delete `.claude/domain-model/rule-effect-classification.md` and
      `characteristic-modifier-caveats.md`; add `.claude/domain-model/ability-classification-catalogue.md`
- [ ] 6.2 Update `statline-flag-rules.md`, `invulnerable-save-effect-resolution.md`,
      `bsdata-json-ingestion.md`, `roster-context.md`, `army-list-import-pipeline.md`,
      `battlescribe-import-pipeline.md`, `characteristic-modification-kind.md`,
      `characteristic-value-domain-model.md`, `design-tokens.md` where they reference the baseline,
      regex classifier or candidates
- [ ] 6.3 Update the topic index in root `CLAUDE.md` and `.claude/domain-model-11e.md`
- [ ] 6.4 `.claude/vnext-ideas.md`: remove the regex-classifier-based entries; add phase/turn ability
      highlighting, condition toggles, consumers for the four data-only effect kinds, a display for
      conditional Statline effects, and the Detachment-rule weapon-effect asymmetry (D7)
- [ ] 6.5 In `classify-abilities-via-llm-batch`, amend the proposal's "old tool untouched" non-goal
      and mark tasks 5.3/5.4 superseded by this change
- [ ] 6.6 At archive, delete `openspec/specs/rule-effect-classification/` and
      `openspec/specs/rule-effect-classification-baseline/` if archiving leaves them empty

## 7. After the full-corpus run

- [ ] 7.1 `collect` → `resolve` → `export`; commit the regenerated catalogue file and confirm 3.3 still
      passes
- [ ] 7.2 Re-point the two real-corpus tests from 4.5 at the real catalogue file where their texts are
      now classified
- [ ] 7.3 Verify `/LivePlay` against the real Black Templars capture via chrome-devtools (Statline
      flags, weapon mutations, unresolved references, Detachment-rule attachments)
