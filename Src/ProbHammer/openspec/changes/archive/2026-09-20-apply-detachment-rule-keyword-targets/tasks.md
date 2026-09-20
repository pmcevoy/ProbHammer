## 1. Domain model additions

- [x] 1.1 Add `AbilityOrigin.DetachmentRule` to `src/ProbHammer.Core/Domain/Catalogue/AbilityOrigin.cs`,
  with a doc comment noting (per `roster-model`'s Inbound Detachment-Rule Abilities requirement)
  that — unlike every other `AbilityOrigin` value — it is never produced by Datasheet resolution,
  only by the roster-enrichment matching step added in section 2.
- [x] 1.2 Add `InboundAbilities` to `ICombatUnit` (`src/ProbHammer.Core/Domain/Roster/ICombatUnit.cs`)
  as a mutable `IReadOnlyList<Ability>` property, mirroring `IsHalfStrengthOverride`/
  `IsBattleShocked`'s exact `{ get; set; }` shape. Implement on `Unit` and `AttachedUnit`, both
  defaulting to `[]`.
- [x] 1.3 Unit test: a freshly-constructed `Unit` and `AttachedUnit` both have an empty
  `InboundAbilities` (spec scenario: "Default is empty").

## 2. Shared Detachment-rule keyword matching helper

- [x] 2.1 Add `DetachmentRuleInboundAbilityResolver` to `Domain.Roster`
  (`src/ProbHammer.Core/Domain/Roster/DetachmentRuleInboundAbilityResolver.cs`), independent of
  `Domain.Catalogue.Bsdata` (per design.md D5 — same precedent as `ArmyRuleNameLookup`/
  `InvulnerableSaveCaveatClassifier`).
- [x] 2.2 Implement `Apply(IReadOnlyList<ICombatUnit> units, IReadOnlyList<ResolvedDetachment>
  detachments, RuleClassificationBaseline baseline)`: for each `DetachmentRule` across every
  `ResolvedDetachment`, normalize its Text (`RuleEffectClassifier.Normalize`) and look it up via
  `baseline.TryGet` — never a live `RuleEffectClassifier.Classify` call (per design.md D1, mirroring
  `ApplyStatlineFlagRules`'s own lookup convention). When a match exists and its `Target` is a
  `KeywordRuleTarget`, append one synthesized `Ability { Name = rule.Name, Text = rule.Text, Scope =
  AbilityScope.Unit, Origin = AbilityOrigin.DetachmentRule }` to `InboundAbilities` for every unit in
  `units` whose `KeywordResolution.EffectiveKeywords(unit)` contains that keyword (case-insensitive,
  matching `EffectiveKeywords`'s own comparer). A rule with no baseline entry, or whose matched
  `Target` is not `KeywordRuleTarget`, attaches nothing. Mutates each matched unit's
  `InboundAbilities` in place (a fresh empty list per D1/task 1.2, since this runs once per
  `ArmyRosterProvider.Build` call against freshly-constructed units).
- [x] 2.3 Unit tests (`DetachmentRuleInboundAbilityResolverTests`), covering every
  `army-roster-enrichment` "Detachment Rule Keyword Target Resolution" scenario: a keyword-matched
  rule attaches to a matching unit; a non-matching unit is unaffected; an `AttachedUnit` gains the
  match via any one present component's own keyword, not just the Bodyguard's; a rule with no
  baseline entry attaches nothing; a rule whose matched `Target` is `SelfRuleTarget` or
  `UnconditionalRuleTarget` attaches nothing.

## 3. Wire the matching step into roster building

- [x] 3.1 Add a `RuleClassificationBaseline` constructor parameter to `ArmyRosterProvider`
  (`src/ProbHammer.Web/Services/ArmyRosterProvider.cs` — already a registered DI singleton per
  `Program.cs`, no new registration needed).
- [x] 3.2 In `ArmyRosterProvider.Build`, call `DetachmentRuleInboundAbilityResolver.Apply(roster.Units,
  roster.Detachments, baseline)` once, after `BuildFromText`/`BuildFromBattleScribe` each construct
  their own `ArmyRoster`, before returning the `ArmyRosterBuildResult` (per design.md D5 — one call
  site shared by both pipelines, no changes to `ArmyRosterEnricher.Enrich` or
  `BattleScribeRosterMapper.Map` themselves).
- [x] 3.3 Test: both `BuildFromText` and `BuildFromBattleScribe` paths produce a roster whose matching
  units carry the expected `InboundAbilities` entry (spec scenario: "Both import pipelines produce
  identical InboundAbilities for equivalent input") — a hand-built `ParsedArmyList`/BattleScribe JSON
  fixture pair describing the same Detachment + matching unit, asserting equal `InboundAbilities`
  results from each.

## 4. Render InboundAbilities in the aggregate ability view

- [x] 4.1 In `AttachedUnitAggregator.BuildAbilities`
  (`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs`), after the existing per-component
  walk and `PromoteArmyRuleAbilities` call, add one `AggregateAbilityEntry(ComponentName: null,
  StatlineName: null, ability)` per entry in `combatUnit.InboundAbilities` — read directly from the
  `ICombatUnit`, not derived from or promoted out of any per-component entry (per the
  `attached-unit-tracker` delta's "Aggregate Ability View" requirement).
- [x] 4.2 Unit tests: an `InboundAbilities` entry renders belonging to no single component (spec
  scenario: "An inbound Detachment-rule ability is reported belonging to no single component");
  it disappears once every component of the combat unit reaches a remaining count of 0 (spec
  scenario: "An inbound Detachment-rule ability disappears once the whole combat unit is gone").

## 5. Apply a matched rule's Effect as WholeUnit-scoped

- [x] 5.1 In the `statline-flag-rules` bearer-scope check (`AttachedUnitAggregator
  .TryGetApplicableEntry`), widen the guard so an ability whose `Origin` is `AbilityOrigin
  .DetachmentRule` is treated as a match regardless of its own baseline entry's `Target` (today's
  guard: `entry.Target is SelfRuleTarget or AttachedUnitRuleTarget`).
- [x] 5.2 In `IsBearerOf`, ensure a `DetachmentRule`-origin match resolves to the same unconditional
  `true` the existing `AttachedUnitRuleTarget` branch already returns (per design.md D4 — reuse the
  existing WholeUnit semantic, don't add a new branch) — since eligibility was already decided by
  section 2's roster-wide match, this downstream check should not re-derive scope from the baseline
  entry's own `Target`.
- [x] 5.3 Unit test: a fixture unit carrying a `DetachmentRule`-origin ability whose baseline entry is
  `KeywordRuleTarget` + a resolvable `ScalarCharacteristicEffect` (mirroring Faith-Fuelled Resolve's
  real shape: `Improve Oc 1`) has every present statline row's Oc flagged one higher than the
  Datasheet's base value (spec scenario: "A Detachment-Rule-origin ability applies as WholeUnit-scoped
  despite its own keyword-classified target").
- [x] 5.4 Regression test: an ability whose Origin is NOT `DetachmentRule` but whose baseline entry is
  still `KeywordRuleTarget`-classified continues to produce no flagged value (existing "A keyword-scoped
  match produces no flagged value" scenario, now explicitly scoped by Origin).

## 6. Real-corpus verification

- [x] 6.1 Confirm the checked-in baseline entry for Faith-Fuelled Resolve
  (`src/ProbHammer.Web/Data/RuleEffectClassifications.json`) is exactly `KeywordRuleTarget("SWORD
  BRETHREN SQUAD")` plus an uncaveated `Improve Oc 1` — read the actual entry directly rather than
  relying on this proposal's own restated summary, since a hand-typed baseline `text` mismatch (a
  known trap documented in `.claude/domain-model/rule-effect-classification.md`) would silently make
  section 2's lookup miss.
  **Found, corrected from this task's own phrasing**: `Target`/`Effects` are exactly as expected
  (`KeywordRuleTarget("SWORD BRETHREN SQUAD")` + `Improve Oc 1`), and `Text` matches the real BSData
  rule description verbatim (confirmed against `src/ProbHammer.Web/BsData/Imperium - Space
  Marines.json` line ~80151, the "Marshal's Household" Detachment's own locally-declared `rules[]`
  entry) - but the entry is NOT uncaveated: `isCaveated: true`, `fullyHandled: true` (the real rule
  text carries a trailing "Restrictions: ..." composition clause after the OC grant, which
  `RuleEffectClassifier` correctly leaves unextracted, and a human has verified that leftover names
  no further game effect). This is harmless for this change: neither
  `DetachmentRuleInboundAbilityResolver.Apply` nor `AttachedUnitAggregator.TryGetApplicableEntry`
  (the Statline path) filter on `IsCaveated` - only `TryGetWeaponEffectEntry` (BuildWeapons) does,
  and this change never touches that path. Confirmed the real Sword Brethren Squad datasheet
  (`Imperium - Black Templars.json` line ~1558) still carries the matching keyword.
- [x] 6.2 Run the app (`docker compose up` or `dotnet run`) against a real captured Black Templars
  export that selects the "Marshal's Household" Detachment and includes a Sword Brethren Squad unit;
  confirm on `/LivePlay` that the Sword Brethren Squad's unit block shows an orphaned "Faith-Fuelled
  Resolve" ability (popover rendering the real rule text) and its OC tile reads one higher than the
  Datasheet's printed base value — per this project's standing practice of a real-app check before
  calling an `AttachedUnitAggregator`-touching change done.
  **Confirmed live**: ran `dotnet run --project src/ProbHammer.Web`, imported
  `data/gw-app-export-templars-latest.txt` (real capture already carrying "Marshal's Household" +
  "Sword Brethren Squad") via `/Import`, fetched `/LivePlay`. The "Sword Brethren Squad with Marshal"
  AttachedUnit block shows an orphaned "Faith-Fuelled Resolve" entry (`spans-whole-unit` cell, same
  slot as the "Templar Vows" Army Rule promotion above it, popover rendering the real rule text incl.
  the Restrictions clause) and BOTH present statlines (Marshal's own row and Sword Brethren's own
  row) show an `OC*` flagged tile via `statline-flag-legend`, confirming WholeUnit-scope application
  reached the whole combined unit, not just one component.
  **Two real bugs found and fixed via this same live check, post-ship (two rounds of live user
  report)** — see `.claude/implementation-notes.md`'s "`WholeUnitAbilitySpans` — a ComponentName-null
  Ability Entry's Own Liveness and Merged Cell" for the full writeup:
  1. The `InboundAbilities`-sourced `AggregateAbilityEntry` was originally built with
     `ContributingComponentNames: []`; `LivePlayModel.BuildWholeUnitAbilitySpans`' own `IsFullyDead`
     check (`.All()` over a now-empty filtered sequence) is vacuously `true` on an empty input, so
     "Faith-Fuelled Resolve" rendered `data-dead="true"`/`run-collapsed` (invisible) despite the OC
     bump correctly applying. Fixed by listing every one of the combat unit's own component names.
  2. Fixing (1) exposed a second, pre-existing bug: `BuildWholeUnitAbilitySpans` grouped
     `ComponentName: null` entries by `Ability.Name`, rendering one cell per distinct ability name - a
     first attempt gave each its own `grid-row`, but direct user correction clarified the actually
     wanted shape: every whole-unit-scoped ability should stack inside ONE cell, the same shape a
     single component's own Datasheet-abilities cell already uses. Fixed by collapsing
     `BuildWholeUnitAbilitySpans` to return at most one merged span (still deduped by name as a safety
     net, but no longer grouped into separate cells), with `IsFullyDead` computed from the union of
     every contained ability's own `ContributingComponentNames`.
  Both fixes verified against the same real export post-fix: "Templar Vows" and "Faith-Fuelled
  Resolve" now render together inside one `spans-whole-unit` cell at `grid-row: 1`, with the statline
  rows correctly shifted to `grid-row: 2`.
- [x] 6.3 Confirm a non-Sword-Brethren unit in the same roster is unaffected (no "Faith-Fuelled
  Resolve" entry, no OC change) — the real-corpus counterpart to task 2.3's "non-matching unit is
  unaffected" fixture test.
  **Confirmed live**: the rendered `/LivePlay` page has 5 unit blocks total; "Faith-Fuelled Resolve"
  and every `stat-tile-flagged` OC tile appear only within the "Sword Brethren Squad with Marshal"
  block - the other 4 unit blocks show neither.
- [x] 6.4 Run the full test suite and confirm no unrelated regression.
  **Confirmed**: `dotnet test tests/ProbHammer.Tests/ProbHammer.Tests.csproj` - after both fixes
  above, 672 succeeded (2 new regression tests added: `AttachedUnitAggregatorTests`'
  `ContributingComponentNames` assertion, `LivePlayAbilityRenderingTests.
  TwoSimultaneousComponentLessAbilities_RenderAtDistinctGridRows_NeitherHidingTheOther`), 0 failed,
  14 skipped (pre-existing, explicitly-filtered full-corpus-scan tests that require the external
  live BSData clone, unrelated to this change).

## 7. Docs

- [x] 7.1 Update `.claude/domain-model/roster-context.md` (covers `ICombatUnit`/`AttachedUnitAggregator`)
  with `InboundAbilities`' shape and the new "Aggregate Ability View" source.
- [x] 7.2 Update `.claude/domain-model/army-list-import-pipeline.md` (or the appropriate enrichment doc)
  with the new `DetachmentRuleInboundAbilityResolver` step and its `ArmyRosterProvider.Build` call
  site.
