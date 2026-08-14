## 1. Markup: run/span linkage attributes

- [x] 1.1 In `LivePlay.cshtml`, add `data-run-index="@runIndex"` to each run's `col-statline` cell.
- [x] 1.2 Add `data-run-index="@runIndex"` to each run's per-row `col-model-abilities` and
      `col-unit-abilities` cells (the ones rendered inside the `runIndex` loop, not the
      `ComponentAbilitySpans` loop).
- [x] 1.3 Add `data-first-run-index="@(span.FirstRunIndex)"` and
      `data-last-run-index="@(span.LastRunIndex)"` to the `.spans-component` `col-model-abilities`
      and `col-unit-abilities` cells rendered from `unit.ComponentAbilitySpans`.

## 2. JS: per-run collapse computation

- [x] 2.1 In `live-play.js`, add a helper that determines whether a single `.statline-entry` is
      fully deselected (single-toggle: `deselected.has(key)`; group header: every loadout's key is
      in `deselected`), factored out of `updateIndicators()` so both it and the new collapse logic
      share one definition.
- [x] 2.2 Build a run→entries map once in `initUnitSelection()` (parallel to the existing `entries`
      array), grouping `.statline-entry` elements by their nearest ancestor `[data-run-index]`.
- [x] 2.3 Add `updateRunCollapse()`: for each run index, compute `runCollapsed` = every entry in
      that run is fully deselected (2.1); toggle a `run-collapsed` class on the run's `col-statline`
      cell and its per-row `col-model-abilities`/`col-unit-abilities` cells (if present) to match.
- [x] 2.4 In the same function, for each `[data-first-run-index]` / `[data-last-run-index]` cell,
      toggle `run-collapsed` when every run index in that inclusive range is collapsed (per 2.3);
      leave expanded if any run in range has at least one selected or partial entry.
- [x] 2.5 Call `updateRunCollapse()` from `refresh()` alongside `updateIndicators()` and
      `recomputeWeaponSections()`.

## 3. CSS: collapsed presentation

- [x] 3.1 Add `.statline-cell.col-statline.run-collapsed .statline-tiles` rules that hide the
      stat-tile (`max-height: 0; overflow: hidden;` plus whatever margin/opacity is needed for a
      clean collapse), leaving `.statline-header`/`.loadout-breakdown` content untouched.
- [x] 3.2 Add `.statline-cell.col-model-abilities.run-collapsed`,
      `.statline-cell.col-unit-abilities.run-collapsed` rules collapsing the full cell (max-height,
      padding, border together) to a near-zero footprint.
- [x] 3.3 Add a transition on the properties touched by 3.1/3.2 so collapse/expand animates rather
      than snapping instantly, consistent with the rest of the page's existing `0.15s` transitions.

## 4. Verification

- [x] 4.1 Manually exercise the example army in `/LivePlay`: deselect a single-entry run (e.g. a
      unique character) and confirm its stat-tile collapses to header-only while its row-bound
      ability cells (if any) collapse too.
- [x] 4.2 Deselect only one entry of a multi-entry run (e.g. two statlines sharing one stat-tile)
      and confirm the stat-tile stays fully expanded; then deselect the remaining entry and confirm
      it collapses.
- [x] 4.3 Deselect one loadout of a multi-loadout entry (leaving it partial) and confirm its run
      does not collapse.
- [x] 4.4 For a component with a `ComponentAbilitySpans` entry covering multiple runs, deselect runs
      one at a time and confirm the spanning ability cell stays expanded until the last run in its
      range is deselected, then collapses; reselect one run and confirm it re-expands immediately.
- [x] 4.5 Confirm the Ranged/Melee sections' filtering behavior (which weapons show/hide) is
      unchanged by this change - no regression in `recomputeWeaponSections()` behavior.
- [x] 4.6 Run the existing test suite (`dotnet test`) to confirm no regressions in
      `LivePlayModelTests.cs` or other affected tests.
