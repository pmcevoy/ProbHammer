## 1. JSON model and gating evaluator

- [x] 1.1 Add `Modifiers` (`List<BsModifier>`, default `[]`) to `BsRule`
  (`src/ProbHammer.Core/Domain/Catalogue/Bsdata/Json/BsCatalogueFile.cs`) — currently silently
  dropped.
- [x] 1.2 Generalize `BsdataDatasheetMapper.IsProvablyAlwaysTrueInMatchedPlay`'s per-condition
  evaluation: keep the existing `scope: "force"`/`"roster"` behavior (membership in
  `ctx.GameModeGateIds`, type-agnostic) unchanged, and add a `scope: "primary-catalogue"` branch
  that evaluates the condition's own `Type` against a new `ctx.PrimaryCatalogueId` — `notInstanceOf`
  true when `ChildId != PrimaryCatalogueId`, `instanceOf` true when `ChildId == PrimaryCatalogueId`,
  any other type at this scope unknowable. Any other scope stays unknowable, unchanged.
- [x] 1.3 Add `PrimaryCatalogueId: string?` to `WalkContext`, threaded through a new optional
  trailing `BuildDatasheet` parameter (default `null`, matching `forceEntries`/`glossary`'s
  existing pattern).
- [x] 1.4 In `ResolvedBsdataCatalogue.ResolveDatasheet`, pass `Closure.Files[0].Catalogue.Id` into
  the new parameter.

## 2. Apply gating to Core Rule extraction

- [x] 2.1 Thread the resolved rule's own `Modifiers` from `RuleGlossary`/`RuleDefinition` (or the
  underlying `BsRule` resolution) through to `ProcessRuleInfoLink`, and call the (now-generalized)
  gating evaluator against them before constructing the `Ability`. A gated reference produces no
  `Ability` — same silent-skip treatment as an unresolvable rule name.
- [x] 2.2 Confirm existing entry/group-level `IsGameModeGated` call sites are unaffected — same
  inputs, same outputs, since the new `scope: "primary-catalogue"` branch only fires for a
  condition scope real entry/group modifiers have never used.
  **Found and fixed a real bug during this verification**: `IsGameModeGated`'s early-return guard
  (`if ctx.GameModeGateIds.Count == 0 return false`) short-circuited before ever reaching the new
  primary-catalogue check whenever `forceEntries` wasn't supplied (i.e. the fixture-test case)
  even though `PrimaryCatalogueId` was set — fixed to `if (... && ctx.PrimaryCatalogueId is null)`.
  `GameModeGatingTests` (existing) still fully green — confirmed no regression to the original
  behavior.

## 3. Gating tests

- [x] 3.1 Extend `core-rule-ability.json` (or a new fixture) with a `BsRule` carrying a
  `primary-catalogue`-scoped `hidden` modifier (mirror the real Oath of Moment/Templar Vows
  shapes — both an `instanceOf`-style and a `notInstanceOf`-style case), resolved against two
  different synthetic starting-catalogue ids.
- [x] 3.2 Add tests asserting opposite inclusion for the two starting catalogues, and that an
  unrecognized gating shape does not exclude the reference.
- [x] 3.3 Add a small, `[Fact(Explicit = true)]` (needs the live clone) regression test resolving
  `Scout Squad` against both `Imperium - Black Templars.json` and `Imperium - Salamanders.json`,
  asserting `Templar Vows` only for the former and `Oath of Moment` only for the latter — this is
  the concrete real-world case this change fixes; keep it from silently regressing.
  **Confirmed passing against the real live clone**: Black Templars → Templar Vows only, no Oath
  of Moment; Salamanders → Oath of Moment only, no Templar Vows.

## 4. Dedup and component-less placement

- [x] 4.1 Make `AggregateAbilityEntry.ComponentName` meaningfully optional (`string?`).
  Also added `ContributingComponentNames` (empty except for a deduped entry) to support the
  collapse rule.
- [x] 4.2 In `AttachedUnitAggregator.BuildAbilities`, after building the normal per-component
  entries, detect `CoreRule`-origin entries sharing an identical `Name` (case-insensitive) across
  two or more present components of the same `AttachedUnit`, and collapse them into one entry with
  `ComponentName: null` (and no `StatlineName`) in place of the per-component ones. No change for a
  standalone `Unit`'s own single component beyond the same detection running (a no-op there).
  Collapse-on-death falls out for free from per-request rebuilding rather than needing separate
  state (see the method's own doc comment).
- [x] 4.3 Confirm this dedup step does not touch any non-`CoreRule`-origin ability, and does not
  touch a `CoreRule` ability that only one component contributes.

## 5. Rendering

- [x] 5.1 In `_UnitBlock.cshtml`, add a rendering branch for a `ComponentName: null` entry: its own
  row above every component's statline rows, not aligned to or spanning any one component's rows.
  Implemented via a `rowOffset` (1 when any whole-unit span exists, else 0) added to every other
  cell's `grid-row`, freeing row 1 for the new `.spans-whole-unit` cell. New
  `WholeUnitAbilitySpanViewModel`/`BuildWholeUnitAbilitySpans` in `LivePlay.cshtml.cs`;
  `BuildComponentAbilitySpans` updated to exclude `ComponentName == null` entries (would otherwise
  crash on `.Min()`/`.Max()` over an empty run-index list).
- [x] 5.2 Implement the component-less collapse rule: hidden only once every contributing
  component's every model-line has `RemainingCount == 0` — needs the entry to track which
  components contributed to it (from step 4.2) to evaluate this correctly, distinct from the
  existing per-component-span collapse rule.

## 6. Rendering/aggregation tests

- [x] 6.1 `AttachedUnitAggregatorTests`-style coverage: a `CoreRule` ability shared by all present
  components collapses to one entry; shared by only some components still collapses to one entry;
  persists while any contributing component is present; disappears once every contributing
  component is fully dead.
- [x] 6.2 A real-Razor-path rendering test (mirroring `LivePlayAbilityRenderingTests`'s existing
  pattern) confirming a component-less entry renders once, above the block, and collapses per the
  new rule.

## 7. Verification against the real bug report

- [x] 7.1 Run the existing spike-style pipeline against `data/gw-app-export-templars.txt` and
  confirm: `Oath of Moment` no longer appears anywhere in this Black Templars roster; `Templar Vows`
  appears exactly once per `AttachedUnit` (not once per component) and exactly once on the
  standalone `Impulsor`/`Scout Squad` entries (where it already only had one component).
  **Confirmed exactly**: every AttachedUnit shows one deduped `Templar Vows` with the correct
  contributor list; no `Oath of Moment` anywhere; standalone Impulsor/Scout Squad show `Templar
  Vows` as a normal single-component entry; `Deadly Demise D3`/`Firing Deck 6`/`Infiltrators`/
  `Scouts 6"`/Shield Dome/Enhancement all unaffected.
- [x] 7.2 `docker compose up` + a real browser session against the same export: confirm the
  deduped `Templar Vows` renders once, above each attached-unit block, correctly positioned; confirm
  `Oath of Moment` is gone; confirm nothing else (Shield Dome, Enhancements, other Core rules like
  `Deadly Demise D3`/`Firing Deck 6`) regresses.
  **Confirmed via `docker compose up --build` + Firefox.** `Templar Vows` renders once per
  attached-unit block with `spans-whole-unit` class, its own row above every component's rows,
  visually matching the user's own ASCII mockup exactly (screenshot compared side-by-side).
  `Oath of Moment` absent everywhere. Scout Squad/Impulsor's own `Templar Vows` renders as a
  normal `spans-component` entry (no dedup needed, single component). Shield Dome, `✦ Oathbound
  Exemplar`, and every other ability unaffected.
- [x] 7.3 Full existing test suite green, plus every explicit-only corpus scan re-run manually and
  clean (including the new gating regression test from 3.3).
  **356 total (350 run + 6 explicit-only), all passing** — default suite via `dotnet test`, every
  explicit test (including the new `PrimaryCatalogueGatingTests`/`ChapterExclusiveCoreRuleRegressionTests`
  and every pre-existing corpus scan) re-run manually via the built exe, all clean.

## 8. Docs

- [x] 8.1 Update `.claude/domain-model-11e.md`'s "Core Rule Ability Extraction" and
  `IsGameModeGated` sections to document the generalized gating evaluator and the new
  component-less aggregate placement.
- [x] 8.2 Update `PROGRESS.md` per this project's standing documentation-maintenance instruction —
  including a note that this change fixes real over-extraction the `resolve-core-rule-abilities`
  entry's own description should be read alongside, not a correction to that entry itself.

## 9. Post-Delivery Correction (found via user feedback after 7.2's own verification)

- [x] 9.1 `AbilityOrigin` gains `ArmyRule`; `BsdataDatasheetMapper` gains
  `HasPrimaryCatalogueScope` (a structural presence check on a rule's own gating, independent of
  the exclusion evaluator) and uses it in `ProcessRuleInfoLink` to classify Origin as `ArmyRule`
  vs plain `CoreRule` — replacing the "shared by 2+ present components" heuristic, which never
  triggered for a standalone Unit even though an army-wide fact is just as much one there.
- [x] 9.2 `AttachedUnitAggregator.PromoteArmyRuleAbilities` (renamed from
  `DedupeSharedCoreRuleAbilities`) now promotes on `Origin == ArmyRule` unconditionally — even a
  single contributor gets `ComponentName: null` — not on a contributor headcount.
- [x] 9.3 Fixed the original, still-unfixed "Shield Dome invisible behind Transport/Assault
  Vehicle" collision: `LivePlay.cshtml.cs`'s `BuildComponentAbilitySpans` now returns an adjusted
  `statlineBlocks` alongside its spans, absorbing a single-row component's row-bound abilities
  into its own component-wide span instead of rendering both as separately-positioned,
  identically-coordinated grid cells.
- [x] 9.4 Updated tests: `AttachedUnitAggregatorTests`'s existing dedup tests switched from
  `CoreRule` to `ArmyRule`, one assertion updated for unconditional promotion, one new test for
  the standalone-Unit case; new `PrimaryCatalogueGatingTests` Origin assertions; new
  `LivePlayAbilityRenderingTests` case for the absorption merge. Full suite green (360 total, 354
  run + 6 explicit-only); every explicit-only test re-run manually and clean.
- [x] 9.5 Re-verified via `docker compose up` + a real browser session against the real Templars
  export: Impulsor now shows `Templar Vows` in its own box above **and** `Shield Dome` alongside
  `Transport`/`Assault Vehicle`/`Deadly Demise D3`/`Firing Deck 6`, all visible together; the
  already-verified `AttachedUnit` rendering unchanged (screenshot compared, identical).
- [x] 9.6 Updated this change's own `design.md` (new "Post-Delivery Correction" section) and
  `specs/` deltas (`attached-unit-tracker`, `live-play-view`, `catalogue-json-ingestion`, new
  `datasheet-catalogue` delta) to reflect the corrected behavior rather than leave the
  now-inaccurate "shared by 2+"/plain-`CoreRule` language in place.
