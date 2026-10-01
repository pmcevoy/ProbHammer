## Context

See proposal.md - Why. Current runtime shape, for the parts this change touches:

- `RuleClassificationBaseline` (`Domain/Catalogue/`) is loaded once in `Program.cs` from
  `Data/RuleEffectClassifications.json` (missing file → empty) and threaded through
  `LivePlay.cshtml.cs`, `LivePlayCasualtyService` and `ArmyRosterProvider` into
  `AttachedUnitAggregator.Build` and `DetachmentRuleInboundAbilityResolver.Apply`. Every lookup is
  `baseline.TryGet(RuleEffectClassifier.Normalize(ability.Text))`.
- `AttachedUnitAggregator` has four consumers of a matched entry: `ResolveCaveatedInvulnerableSaves`,
  `ApplyStatlineFlagRules` (applies regardless of `IsCaveated`), `ResolveContributionProfile` /
  `ResolveAttacksContributions` (only `!IsCaveated`), and `FindUnresolvedAbilities` (only
  `IsCaveated`). Statline and weapon paths both exclude `Keyword`/`Unconditional` targets; the
  Statline path alone excepts `AbilityOrigin.DetachmentRule`.
- The pipeline's extractor keys records on `SHA256(UTF-8(RuleEffectClassifier.Normalize(text)))`
  (`tools/AbilityPipeline/Extractor/ContentHash.cs`). `Normalize` only folds U+2019 → `'` and U+00A0
  → space.
- The classifier's own POCOs (`tools/AbilityPipeline/Classifier/Models/`) deliberately mirror Core's
  discriminators (`Self`/`AttachedUnit`/`Keyword`/`Unconditional`, `AllWeapons`/`WeaponClass`/
  `NamedWeapon`), with three differences: `KeywordTarget.Keywords` is a list, the weapon effect's
  discriminator is `WeaponCharacteristic` (Core: `Weapon`), and `InvulnerableSaveEffect` is flat
  `(MeleeInSv, RangedInSv)` (Core wraps an `InvulnerableSave` of the same two fields).
- `GamePhase`/`GameTurn` live in `Domain.Roster/PhaseTurnSelection.cs`.
- The Dockerfile copies `src/ProbHammer.Web/Data/` wholesale, so a new file there ships without
  build changes.

## Goals / Non-Goals

**Goals:**
- `/LivePlay` behaves as today, driven by the catalogue, for every effect kind it already acts on.
- Every v2 schema field is modeled in Core and loaded, so later features (phase highlighting,
  toggles, keyword/ability grants, Feel No Pain) need no loader or schema work.
- No trace of the regex classifier, baseline, or characteristic-modifier candidates remains in code,
  specs or `.claude/` docs.

**Non-Goals:**
- Any new `/LivePlay` rendering: phase highlighting, condition toggles, a display for a conditional
  Statline effect, and any consumer of `FeelNoPain`/`WeaponKeywordGrant`/`NamedAbilityGrant`/
  `NamedAbilityRemoval` are separate, later changes.
- Changing the Detachment-rule asymmetry below - mimicked as-is.
- Re-running or re-collecting classifications; this change ships with whatever
  `classifications.json` holds when `export` runs (the 50-record sample tonight).

## Decisions

**D1. Move the normalization and hash into Core first, byte-identical.** New static
`AbilityTextKey` (`Domain/Catalogue/`) owns `Normalize` (moved verbatim from `RuleEffectClassifier`)
and `Hash` (moved from the extractor's `ContentHash`: SHA-256 over UTF-8 of the normalized text,
same hex casing). The extractor's `ContentHash` delegates to it. This lands before any deletion, and
the catalogue loader test re-hashes every record's `Text` (spec: "Every record's key is reproducible
from its own text") - the guard that catches any drift, since a drifted hash fails silently at
runtime as "unclassified".

**D2. New Core types, reshaping the kept ones in place (Rider refactors).**
```
AbilityClassification(RuleTarget Target, IReadOnlyList<ClassifiedEffect> Effects,
    IReadOnlyList<ChoiceGroup> ChoiceGroups, IReadOnlyList<GamePhase> Phases,
    GameTurn? TurnOwnership, UsageLimit? UsageLimit, CoverageStatus CoverageStatus,
    string? UnclassifiedResidue)
  + bool IsUnconditional(ClassifiedEffect)           // the spec's one rule, defined once here
ClassifiedEffect(RuleEffect Effect, ConditionBucket ConditionBucket, string? ConditionText,
    ChoiceBranch? ChoiceBranch)
ChoiceGroup(int MinSelect, int MaxSelect, IReadOnlyList<string> Options, string? ConditionText)
ChoiceBranch(int Group, int Option)
ConditionBucket { None, EvaluableNow, Never }   UsageLimit { ... 5 values }
CoverageStatus { Complete, Partial, Unclassifiable }

RuleEffect (renamed from CharacteristicEffect)    // discriminator "kind"
  ScalarCharacteristicEffect             "Scalar"            (unchanged)
  InvulnerableSaveCharacteristicEffect   "InvulnerableSave"  (unchanged)
  WeaponCharacteristicEffect             "WeaponCharacteristic"  (was "Weapon")
  FeelNoPainEffect(int Value, string? Qualifier)                 "FeelNoPain"           new, data-only
  WeaponKeywordGrantEffect(WeaponSelector, string Keyword, string? ReplacesKeyword) new, data-only
  NamedAbilityGrantEffect(string AbilityName)                    "NamedAbilityGrant"    new, data-only
  NamedAbilityRemovalEffect(string AbilityName)                  "NamedAbilityRemoval"  new, data-only
RuleTarget: KeywordRuleTarget(string) → KeywordRuleTarget(IReadOnlyList<string> Keywords)
```
`CharacteristicEffect` → `RuleEffect` because four of the seven kinds aren't characteristic effects;
`RuleEffect` pairs with the existing `RuleTarget`. Everything else keeps its name, since the
resolvers, `CharacteristicModificationKind` and their tests reference them. The four new kinds are
plain records with no behavior - per the user's decision, they exist so the loader accepts every
real record (spec: "unrecognized effect kind fails the load"), not to be consumed. Alternative
considered: a separate JSON-DTO layer mapped onto domain types - rejected, since the export (D4)
already controls the file's shape, so one set of types serves both.

**D3. `GamePhase`/`GameTurn` move to `Domain.Catalogue`.** A Catalogue type can't reference
`Domain.Roster` (Roster depends on Catalogue, not the reverse). Phases and turns are rulebook
concepts, so Catalogue is a fair home; `PhaseTurnSelection` stays in Roster and imports them. Rider
"move to namespace". The Web's global camelCase enum converter is unaffected; the catalogue loader
sets its own camelCase enum converter for `phases`/`turnOwnership`, and `[JsonStringEnumMemberName]`
for `usageLimit`'s spaced values and `evaluable-now`.

**D4. Export writes Core's shape, canonicalized; the classifier still doesn't reference Core.**
`Classifier export` reads `data/classifications.json` + `data/ability-corpus.json` and writes
`src/ProbHammer.Web/Data/ability-classifications.json`:
```
{ "records": [ { "hash", "text", "names": [...], "classification": { ...Core shape... } } ] }
```
sorted by hash, written with the same indented options as the other data files (spec:
deterministic). Core adopts the classifier's `WeaponCharacteristic` discriminator (D2), so the only
shape conversions are `InvulnerableSave {meleeInSv, rangedInSv}` → `{ value: {...} }`, and
canonical-name substitution: for each name the classification carries, look it up in the record's
`Resolution.Resolved` by `(Kind, Verbatim)` and write `Canonical`. `Text`/`Names` come from the
corpus file, for grep-ability and D1's hash check. Contract drift between the classifier's POCOs and
Core is caught by a test that loads the real checked-in file through Core's loader.
Alternative considered: copying `classifications.json` verbatim and resolving names in Core -
rejected, since it moves the pipeline's name-resolution logic into the runtime.

**D5. `AbilityClassificationCatalogue` replaces `RuleClassificationBaseline` at every seam.**
`Load(path)` (missing → `Empty`), `FromRecords(...)` for tests, `TryGet(string abilityText, out
AbilityClassification)` (normalizes + hashes internally, so call sites stop calling `Normalize`).
Registered as a singleton in `Program.cs` under config key `AbilityClassifications:FilePath`
(default `Data/ability-classifications.json`), injected wherever the baseline is today. Load-once
was fine for 41 entries and remains fine for ~3,800 (~4-5 MB).

**D6. One applicability rule; caveat semantics become per effect.** Mapping of today's four
consumers:

| Consumer | Today | After |
|---|---|---|
| `ResolveCaveatedInvulnerableSaves` | first `InvulnerableSave` effect of matched entry | first **unconditional** one |
| `ApplyStatlineFlagRules` | every Scalar/InSv effect, caveated or not | every **unconditional** Scalar/InSv effect |
| `ResolveContributionProfile` / `ResolveAttacksContributions` | effects of a `!IsCaveated` entry | **unconditional** effects |
| `FindUnresolvedAbilities` | effects of an `IsCaveated` entry | **conditional** effects |

Two deliberate behavior differences, both from the per-effect rule: a conditional Statline effect no
longer flags (it used to, if baselined - every baselined one was in fact unconditional, so nothing
real changes), and one record can now both apply and defer on the same weapon (spec scenario "One
record can both apply and defer"). Target scoping (`IsBearerOf`, `Keyword`/`Unconditional`
exclusion) is unchanged.

**D7. Detachment-rule asymmetry mimicked, not fixed.** Today a Detachment-rule ability with a
keyword target flags Statline values (origin exception) but never reaches weapons (the weapon path
filters on target before `IsBearerOf` sees the origin). Kept identical; recorded in
`.claude/vnext-ideas.md` as a candidate fix.

**D8. Deletion order: port, then delete.** Port every consumer onto the catalogue while the legacy
types still compile, delete the legacy layer in one pass, then rewrite docs/specs. Deleting first
would mean stripping the aggregator's apply steps only to re-add them.

**D9. Test fixtures.** `RuleClassificationBaselineFixtures` becomes catalogue fixtures built with
`FromRecords`, keyed by real ability texts (Vexilla, Shield Dome, the weapon-effect texts), so every
ported aggregator/Web test keeps its current assertions. Tests that read the real
`RuleEffectClassifications.json` (`WeaponCharacteristicEffectRealCorpusTests`,
`UnifiedCharacteristicEffectResolutionRegressionTests`) switch to fixtures now; re-pointing them at
the real catalogue file waits for the full run (task 7.x), since the 50-record sample contains none
of their texts.

## Risks / Trade-offs

- **[Risk] Hash drift silently empties the catalogue.** → D1's re-hash test over the real file, plus
  the extractor delegating to the same Core method.
- **[Risk] Unreviewed LLM effects now change displayed values**, including from `partial` records
  (user decision: coverage never gates). → The unconditional rule keeps anything conditional out of
  the applied path; the classification change's own task 5.1 spot-check still runs after the full
  collect.
- **[Risk] Abilities the baseline resolved (Vexilla, Shield Dome, ...) show nothing until the full
  run is exported.** → Accepted by the user; the export after `collect` restores them.
- **[Trade-off] Four effect types with no consumer.** Data-only records are the minimum the loader
  needs; no behavior is added to them (keeps to the "no premature behavior" preference).
- **[Risk] Name canonicalization depends on `Resolution` being current.** A vocabulary refresh
  without `resolve` leaves stale canonical names. → `export` documents running `resolve` first; an
  unresolved name exports verbatim and simply matches less.

## Migration Plan

Single deploy. Rollback is a git revert; the deleted JSON baseline is recoverable from history.
After the full run: `collect` → `resolve` → `export` → commit the regenerated
`Data/ability-classifications.json`.
