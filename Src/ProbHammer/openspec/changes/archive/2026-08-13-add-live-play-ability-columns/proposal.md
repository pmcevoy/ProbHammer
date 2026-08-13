## Why

`/LivePlay`'s Statline area currently renders as a single narrow column of statline blocks, leaving
a lot of unused horizontal space on the card, while abilities render in a separate collapsible
"Abilities" section disconnected from which specific model grants or carries them. Rendering
abilities as two additional columns beside the statlines — one for Model-scoped, one for
Unit-scoped — fills that space and makes it visually obvious what's lost if a specific model is
removed, since each ability sits next to the model row it depends on.

## What Changes

- The Statline section becomes a 3-column layout: STATLINES (unchanged rendering, from
  `merge-adjacent-live-play-statlines`) | MODEL ABILITIES | UNIT ABILITIES.
- Column assignment is by `Ability.Scope` (Model → middle column, Unit → right column). Row
  placement is by ability source, independent of scope:
  - A `ModelLine`-sourced ability (Enhancement-conferred) renders beside that `ModelLine`'s own
    statline row only.
  - A `Datasheet`-sourced ability (e.g. a unit-intrinsic ability like "Righteous Zeal", or a
    Model-scoped attachment-eligibility ability like "LEADER"/"SUPPORT" — none of which are tied
    to one specific model when a component has multiple statlines) renders beside the first
    rendered statline row of its owning component, and visually spans every row belonging to that
    component (not just the first row's own height).
- Only the ability's Name renders in these columns. Full-text-on-demand (e.g. a popup) is
  explicitly deferred to a future change.
- **BREAKING**: `AttachedUnitAggregateView.UnitScopedAbilities` (a flat list combining and
  deduplicating Unit-scoped abilities across every present component of an `AttachedUnit`) is
  removed entirely, with no rolled-up replacement — the columnar page layer reconstructs the
  per-unit view by rendering each component's own abilities independently, so a cross-component
  combined list is no longer needed anywhere.
- **BREAKING**: `AttachedUnitAggregateView.ModelScopedAbilities` is replaced by a new shape
  covering both Model- and Unit-scoped abilities together (`Ability.Scope` already carries which
  is which). Each entry carries the owning component's name, an optional statline name (set for
  `ModelLine`-sourced abilities, absent for `Datasheet`-sourced ones), and the `Ability` itself.
- This also surfaces a previously-hidden gap: `Datasheet`-level abilities with `Scope: Model` (e.g.
  `LEADER`, `SUPPORT`) are not rendered anywhere on `/LivePlay` today — only `ModelLine`-sourced
  Model-scoped abilities are. This change renders them for the first time, in the new Model
  Abilities column, using the same first-row/rowspan placement as `Datasheet`-sourced Unit
  abilities.
- The existing separate "Abilities" collapsible section on `/LivePlay` is removed — redundant once
  abilities render inline in the Statline area.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `attached-unit-tracker`: the "Aggregate Ability View" requirement is replaced — the combined
  cross-component Unit-scoped list and the `ModelLine`-grouped-only Model-scoped list are both
  superseded by one per-component, optionally row-bound aggregation covering both scopes,
  including previously-unrendered `Datasheet`-level Model-scoped abilities.
- `live-play-view`: the "Ability Section Rendering" requirement is removed (its separate-section
  premise no longer applies). A new requirement describes the 3-column Statline area's ability
  columns: column-by-scope, row-by-source (specific row vs. first-row-with-rowspan), name-only
  rendering. "Statline Section Rendering" gets a small body update noting the two ability columns
  render alongside it — its own grouping/rendering logic is unaffected.

## Impact

- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs` — `UnitScopedAbilities` removed;
  `ModelScopedAbilities` replaced with the new per-component, optionally row-bound shape.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — new/reworked builder walking
  components in `BuildStatlines`'s established display order, collecting each component's
  `Datasheet.Abilities` (component-wide) and each `ModelLine`'s own `Abilities` (row-specific), for
  both scopes, without cross-component combination or deduplication.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` — build the 3-column view-model, matching each
  ability entry to its row or to its component's first-row/rowspan.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml` — 3-column markup; remove the standalone "Abilities"
  `<details>` section.
- `src/ProbHammer.Web/wwwroot/css/site.css` — 3-column grid layout; a CSS-based rowspan-equivalent
  (the page uses `<div>` blocks, not an HTML table, so no literal `rowspan` attribute is available).
- Test coverage: `AttachedUnitAggregatorTests.cs` (new ability-aggregation builder — per-component
  scoping, component/statline-name correctness, `Datasheet` vs. `ModelLine` source distinction,
  both scopes), a `LivePlayModel`-level test for the row/rowspan-matching logic, mirroring the
  precedent set by the existing statline-grouping test.
