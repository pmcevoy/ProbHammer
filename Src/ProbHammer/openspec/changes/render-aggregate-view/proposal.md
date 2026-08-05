## Why

`AttachedUnitAggregator.Build()` (capability `attached-unit-tracker`) already computes the aggregate view — combined statlines, weapons grouped by structural profile with live counts, unit- and model-scoped abilities, effective keywords — but the only way to inspect it today is stepping through a debugger over `src/ProbHammer.Core/Domain/Examples/View.cs`. That file now builds the view for a real, hand-entered army (`Examples/Units.cs`, `Examples/Datasheets.cs`), which makes it possible to actually sanity-check the 11e domain model against a real list for the first time — but only if the result can be seen. A Razor page gives this the layout options (tables, collapsible ability sections) a console dump can't, and doubles as the first screen of the eventual "Live Play" feature (aggregate view + casualty tracking) described in the project's near-term roadmap.

## What Changes

- Add a new Razor page to `ProbHammer.Web` (e.g. `/LivePlay`) that renders the `AttachedUnitAggregateView` for each unit in the example army: statlines, weapons (name, type, key stats, live count), unit-scoped abilities, model-scoped abilities grouped under their owning model-line, and effective keywords.
- The page is read-only for this change — no casualty interaction (tap-to-remove), no simulation hookup. That is deliberately deferred to a follow-on change once the render is proven out.
- Data source is `ProbHammer.Core.Domain.Examples.View.MyArmy()` — the existing hand-built example army — not session state and not a parsed army-list export. This keeps the change scoped to rendering; wiring a real 11e export into this page is separate, deferred work (no 11e `ArmyListParser` exists yet).
- This page is additional and independent of `Index`/`ArmyView` — it does not touch the existing 10e session-based flow, `Enricher`, or `Simulation/*`.

## Capabilities

### New Capabilities
- `live-play-view`: a read-only Razor page in `ProbHammer.Web` that renders one or more `AttachedUnitAggregateView`s — deterministic section ordering (statlines, weapons, unit-scoped abilities, model-scoped abilities, keywords), legible formatting for squad-sized units, and duplicate-valued statlines with different names shown as distinct entries (per `roster-model`'s dedupe-by-name rule). Sourced from static example data for this change.

### Modified Capabilities
- None — `attached-unit-tracker` and the aggregate view shape are unchanged; this only adds a consumer of the existing view.

## Impact

- **New code**: `src/ProbHammer.Web/Pages/LivePlay.cshtml` + `.cshtml.cs`, referencing `ProbHammer.Core.Domain.Roster`/`Examples` types directly (no session, no new API endpoint).
- **Untouched**: `AttachedUnitAggregator`, `Datasheet`/`Unit`/`AttachedUnit`, `Examples/View.cs`, the existing `Index`/`ArmyView` pages, `Enricher`, `Simulation/*`.
- **Tests**: none planned for Razor markup itself (consistent with `Index`/`ArmyView`, which have no view tests); if any C# formatting/grouping logic is extracted into a helper for the page, it gets unit tests against hand-built `AttachedUnitAggregateView` fixtures.
