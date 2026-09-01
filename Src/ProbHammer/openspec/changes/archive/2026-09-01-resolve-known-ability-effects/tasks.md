## 1. Invulnerable-save caveat classifier

- [x] 1.1 Add `InvulnerableSaveCaveatClassifier` (or similar) to `Domain.Catalogue`, implementing
      the four-template exact-whole-string match plus the bare/split resolution rules from
      `invulnerable-save`'s "Footnoted Caveat Text Resolution" requirement, including the
      digit-consistency guard for the split case.
- [x] 1.2 Wire the classifier into `BsdataDatasheetMapper.ResolveInvulnerableSave`/
      `ResolveCaveatAbility`, using it only when it returns a match, keeping today's existing
      fallback unchanged otherwise.
- [x] 1.3 Wire the same classifier into `BattleScribeRosterMapper.ResolveInvulnerableSave`/
      `ResolveCaveatAbility`, replacing that pipeline's own separate digit-collapse logic where it
      overlaps.
- [x] 1.4 Unit tests: bare footnote resolves on exact match (ranged and melee variants); bare
      footnote with unmatched text stays caveated; split footnote resolves the matched side while
      keeping the plain side; split footnote with unmatched text stays caveated; a digit mismatch
      between raw text and matched template stays caveated; a template match with extra
      leading/trailing text stays caveated (the Ensorcelled-Shield-shaped near-miss).
- [x] 1.5 Add a `[Fact(Explicit = true)]` corpus-scan test (mirroring
      `tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/`'s existing pattern) confirming
      the classifier's known-template set still covers every ability text reachable through
      `ResolveCaveatAbility`'s own name-matching convention across the live BSData clone, and that
      the Makari anomaly remains correctly unresolved.

## 2. Drop "Leader"/"Support"/"Attached Unit" abilities

- [x] 2.1 Filter `Datasheet`'s constructor so an ability whose Name is exactly "Leader", "Support",
      or "Attached Unit" is excluded from `Abilities`, regardless of source.
- [x] 2.2 Apply the equivalent exclusion in `BattleScribeRosterMapper`'s own ability extraction
      (Statline/Weapon/Ability Extraction and Core Rule Extraction), per
      `battlescribe-roster-import`'s new requirement.
- [x] 2.3 Unit tests: a local "Leader" ability profile is excluded; a Core-Rule-sourced "Support"
      reference is excluded; an ability with any other name is unaffected; the BattleScribe pipeline
      excludes the same three names from both of its own extraction paths.
- [x] 2.4 Add a `[Fact(Explicit = true)]` corpus-scan test confirming no other real ability in the
      live BSData clone shares these three exact names with different intent.

## 3. Statline-flag rule engine

- [x] 3.1 Define the rule abstraction per design.md's D4 (matches one `AggregateAbilityEntry` by
      exact Ability Name + Text, knows its own target scope — bearer-only vs. whole-unit — and
      produces a flagged characteristic value plus a reference to the matched ability).
- [x] 3.2 Implement the Shield Dome rule (bearer-only scope, populates a flagged invulnerable save).
- [x] 3.3 Implement the Vexilla rule (whole-unit scope, adds 1 to Objective Control on every row).
- [x] 3.4 Wire the rule pass into `AttachedUnitAggregator.Build`, running after
      `BuildStatlines`/`BuildAbilities` produce their live, casualty-filtered results, per design.md's
      D3 — never mutating `Datasheet` or `Unit`.
- [x] 3.5 Confirm liveness: a matched rule's flagged value is re-derived from the currently-present
      abilities on every call to `Build`, and stops applying once its specific bearer (component or
      model-line) is no longer present — no caching independent of this recomputation.
- [x] 3.6 Confirm the matched source ability is unaffected in its own normal Abilities/Enhancements
      rendering — this rule pass never removes or hides it.
- [x] 3.7 Unit tests covering `statline-flag-rules`' three requirements: a matched ability grants/
      adjusts a characteristic; an unmatched ability produces no flag; the source ability still
      renders normally; marking the bearer a casualty removes the flagged value on the next build; a
      casualty elsewhere in the unit leaves an unrelated flagged value untouched; Vexilla's flag
      applies across every row of the unit, not only the bearer's own row/component.

## 4. Unified flagged-stat display

- [x] 4.1 Assign a marker registry per unit block (per render pass), reused for the same source
      ability wherever it recurs, per design.md's D5.
- [x] 4.2 Update `_UnitBlock.cshtml` to append a run's flagged tile's marker to its label (for InSv
      and for any rule-flagged characteristic, e.g. OC).
- [x] 4.3 Replace `.insv-caveat-text`'s always-visible paragraph with a per-run legend listing one
      line per marker present in that run, each line's ability name using the existing
      `RulePopoverRenderer`/`ability-name-line` popover-trigger mechanism.
- [x] 4.4 Ensure a unit-wide-scoped flag's legend line renders in every run it affects, not only the
      bearer's own run, per `live-play-view`'s "A unit-wide-scoped source's legend repeats in every
      affected run" scenario.
- [x] 4.5 Confirm the existing run-collapse rule (fully-deselected / fully-dead) hides a run's
      flagged tile(s) and legend together with its stat-tile, unchanged from today's InSv-only
      behavior.
- [x] 4.6 Update `.claude/design-tokens.md` to describe the marker/legend mechanism in place of the
      retired always-visible caveat-text description.

## 5. Documentation and cleanup

- [x] 5.1 Update `.claude/domain-model-11e.md`'s "BSData JSON Ingestion" section to describe the new
      classifier and the Leader/Support/Attached-Unit exclusion.
- [x] 5.2 Add a `.claude/domain-model-11e.md` (or new doc) section for the `statline-flag-rules`
      mechanism, its liveness contract, and the two seed rules.
- [x] 5.3 Remove the now-addressed portion of `.claude/vnext-ideas.md`'s "Ability-text
      interpretation pass" entry (the InSv-caveat-resolution driver and the Shield-Dome-shaped
      wargear-grant case), leaving the still-deferred parts (general Model/Unit scope
      classification, Leader/Support/Attached-Unit drop-list — now implemented, remove that bullet
      entirely) intact or updated as appropriate.
- [x] 5.4 Update `PROGRESS.md` per this project's standard "Recently Completed" convention once
      implementation lands.
