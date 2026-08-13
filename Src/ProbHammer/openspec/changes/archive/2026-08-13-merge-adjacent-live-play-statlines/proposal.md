## Why

Once statline rows render in Datasheet-declared order (`order-statlines-by-declaration`), a
Crusader Squad's card shows "Sword Brother 1/1" and "Initiate 5/5" back to back with the exact
same M/T/Sv/W/Ld/Oc line repeated underneath each — duplicate information the player has to
visually reconcile at the table. Since those two rows already sit next to each other and share an
identical statline value, they read better as one shared stat-tile with two name/count headers
stacked above it, saving screen space on a page designed to be read on a phone.

## What Changes

- On `/LivePlay`, adjacent statline rows within the same component (Bodyguard, or a single
  Attached unit) that share an identical Statline value (M/T/Sv/W/Ld/Oc, and InSv when present)
  render as one shared stat-tile block, with each contributing entry's own name and
  remaining/initial count kept as its own header line above the shared tile (and its own nested
  loadout breakdown when it has more than one loadout, unchanged from today).
- The merge is adjacency-scoped, not a global by-value regroup: only a contiguous run of entries
  that are already next to each other in the established render order merges. A differently-valued
  statline breaks the run.
- The merge never crosses a component boundary: an Attached unit's entry and the Bodyguard's
  entry never share a stat-tile, even if adjacent and value-equal.
- Each entry's name/count header stays its own distinct element within a merged block — this is
  purely a rendering change, not a data merge. Casualty tracking (a future change) will still need
  to target one specific named entry at a time.
- `AggregateStatlineEntry` gains a `ComponentName` field (populated the same way
  `WeaponContribution.ComponentName` already is: the owning component's `Datasheet.Name`) so the
  page layer can detect component boundaries without re-deriving them. Building on the
  `BuildStatlines` rewrite from `order-statlines-by-declaration` (component-by-component
  iteration) — this is an additive field on an already-being-rewritten method, not a second
  independent rewrite.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `attached-unit-tracker`: "Aggregate Statline View" gains a `ComponentName` field on each
  entry.
- `live-play-view`: "Statline Section Rendering" changes from one row per statline entry to one
  shared stat-tile per contiguous, same-component run of value-identical entries, with each
  entry's name/count kept as its own header line within that block.

## Impact

- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs` — `ComponentName` added to
  `AggregateStatlineEntry`.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `BuildStatlines` populates it.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` — new view-model shape grouping adjacent
  same-component, value-equal `AggregateStatlineEntry` rows.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml` — statline section markup updated to render grouped
  blocks.
- Depends on `order-statlines-by-declaration` landing first (needs its component-ordered,
  per-component `BuildStatlines` rewrite as the base to add `ComponentName` onto).
- Test coverage: `AttachedUnitAggregatorTests.cs` (`ComponentName` populated correctly),
  `LivePlayModel`-level test (grouping: merges adjacent equal-valued same-component entries, stops
  at a value change, stops at a component boundary even when adjacent and equal-valued).
