## Context

See `proposal.md` - Why. The relevant current shape:

- `LivePlay.cshtml`'s Statline area is a CSS grid (`statline-grid`, 3 columns: `col-statline`,
  `col-model-abilities`, `col-unit-abilities`). Each run gets one `grid-row` index; a run's
  `col-statline` cell can contain more than one `.statline-entry` (header line + optional nested
  `.loadout-breakdown`) above one shared `.statline-tiles` stat-tile. `col-model-abilities` and
  `col-unit-abilities` cells are either per-run (same `grid-row` as their run) or
  `.spans-component` (a `grid-row: first / last+1` range covering every run of one component),
  populated respectively from `block.ModelAbilities`/`block.UnitAbilities` per run and from
  `unit.ComponentAbilitySpans` for the spanning case.
- `live-play.js`'s `initUnitSelection()` holds one `Set<string>` of deselected select-keys per unit
  block, keyed by `LivePlayModel.SelectKey`. `updateIndicators()` (called from `refresh()` on every
  toggle click) walks the flat `.statline-entry` list and sets `select-selected` /
  `select-deselected` / `select-partial` on each entry's own toggle element only - it has no notion
  of "run" or of the ability cells at all today. `recomputeWeaponSections()` is the only other
  `refresh()` step; it re-derives Ranged/Melee row visibility from the same `deselected` Set and is
  unaffected by this change.

## Goals / Non-Goals

**Goals:**
- Collapse behavior is a pure rendering consequence of the existing `deselected` Set - no new state
  storage, and no change to what's stored or when it resets (still cleared on page reload).
- `spans-component` cells keep relying on stable `grid-row` indices; collapsing a run never removes
  it from the grid or renumbers rows.
- Correctly handles a multi-entry run (shared stat-tile behind two or more header lines) and both
  row-bound and component-wide ability cells, in both ability columns.

**Non-Goals:**
- No change to `recomputeWeaponSections()` or the Selection-Scoped Weapon Filtering data flow.
- No change to how runs are computed server-side (`ComponentName` + Statline-value grouping) or to
  `ComponentAbilitySpans` - both are existing aggregation-layer concerns, untouched.
- No collapse-state persistence across reload - it derives from `deselected`, which already resets.
- No prescribed animation timing/easing - a CSS transition detail, not a spec-level requirement.

## Decisions

**1. Link DOM cells to their run via `data-run-index` / `data-first-run-index` / `data-last-run-index`
attributes, not by parsing the existing `style="grid-row: ..."` strings in JS.**
Add `data-run-index="@runIndex"` to each run's `col-statline` cell and its per-run
`col-model-abilities`/`col-unit-abilities` cells; add `data-first-run-index`/`data-last-run-index`
to `.spans-component` cells (mirroring `span.FirstRunIndex`/`span.LastRunIndex`, already used to
build their `grid-row` range string). Parsing the inline `grid-row` value in JS would couple
collapse logic to a CSS layout detail and its spanning-range syntax (`"N / M"`); explicit data
attributes are unambiguous and free to diverge from the CSS if the layout ever changes.
*Alternative considered:* a pure-CSS `:has()`-based approach (no JS at all). Rejected because
"every `.statline-entry` inside this specific cell is `.select-deselected`" doesn't have a clean
`:has()` formulation scoped per-cell without also selector-matching across cell boundaries, and it
would introduce a second, parallel mechanism alongside the existing imperative `refresh()` pattern
that already owns every other selection-derived rendering change in this file.

**2. Compute collapse in `refresh()`, alongside the existing `updateIndicators()` /
`recomputeWeaponSections()` steps - not via a `MutationObserver` or a separate event pipeline.**
Add a third step (e.g. `updateRunCollapse()`) called from the same `refresh()` that already
recomputes everything else on every click. Consistent with how the file already treats "recompute
derived rendering from `deselected` on every interaction" as the one pattern for all three
selection-driven concerns (indicators, weapon rows, and now run collapse).

**3. `col-statline` collapses only its `.statline-tiles` sub-element, not the whole cell.**
Every entry's header line (name, count, nested loadout breakdown) stays visible when a run
collapses - that's the "header-only" state described in the proposal, not a further-collapsed
hidden-entirely state. `col-model-abilities`/`col-unit-abilities` cells have no equivalent header
content, so they collapse in full.

**4. A run's collapse condition is "every `.statline-entry` inside it is fully deselected",
computed by grouping entries under their ancestor `[data-run-index]` and reusing the exact
per-entry deselected check `updateIndicators()` already makes** (single-toggle:
`deselected.has(key)`; group header: `loadouts.every(li => deselected.has(li.dataset.selectKey))`)
- just aggregated one level up to the run, instead of adding a second, independent notion of
"deselected." A `spans-component` cell's collapse condition is the same check ANDed across every
run index in its `[data-first-run-index, data-last-run-index]` range.

## Risks / Trade-offs

- [Risk] CSS cannot transition to `height: auto`, so collapsing `col-model-abilities`/
  `col-unit-abilities` (which have no fixed height) means animating `max-height` toward a guessed
  cap rather than the cell's true content height, which can visibly pause or jump on the widest
  cell. → Mitigation: cap at a generously large fixed value (ability names only, per "Statline
  Ability Column Rendering" - short, bounded content), which comfortably covers realistic ability
  counts.
- [Risk] A `spans-component` cell only collapses once every run in its range is individually
  collapsed, so rapidly deselecting several statlines in the same component can cause its ability
  cell to visibly resize on each intermediate click rather than once at the end. → Mitigation:
  this is the same per-click recompute cost the file already accepts for
  `recomputeWeaponSections()`; no new performance concern, just a cosmetic one already implied by
  the spec's "stays expanded until every spanned row is deselected" rule.
- [Risk] Any future `.statline-cell` variant added to `LivePlay.cshtml` that omits the
  `data-run-index`/`data-first-run-index`/`data-last-run-index` attributes fails silently - the
  cell simply never collapses, rather than erroring. → Mitigation: the spec's row-bound and
  spanning ability-collapse scenarios exercise every cell kind that exists today; a manual pass
  against the example army after this change (and after any future new cell kind) should confirm
  collapse fires for it too.

## Migration Plan

Pure front-end change (Razor markup attributes, CSS, and `live-play.js`) - no session-shape,
domain-model, or persisted-data change. Ships as a normal deploy; no rollback considerations beyond
reverting the commit.

## Open Questions

- Should the still-visible header line(s) of a collapsed run also tighten their own vertical
  spacing (e.g. `margin-bottom` currently sized for the stat-tile below them), or keep their
  current spacing as-is once the tile beneath them disappears? Doesn't affect the collapse
  condition or any spec scenario - a visual-polish call for whoever implements the CSS.
