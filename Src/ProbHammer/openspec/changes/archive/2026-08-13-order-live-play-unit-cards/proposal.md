## Why

`/LivePlay` currently renders unit-card blocks in whatever order `Examples.View.MyArmy()`
happens to return them — an incidental artifact of how the example army list was typed in, not a
useful ordering at the table. Players want the biggest, most attention-heavy units (and units
with an attached leader/character) surfaced first.

## What Changes

- Unit-card blocks on `/LivePlay` are reordered: units built from an `AttachedUnit` source sort
  before units built from a plain `Unit` source; within each of those two groups, units sort by
  descending total model count (largest unit first).
- "Total model count" is the whole unit's count — for an `AttachedUnit`, this includes the
  Bodyguard plus every Attached unit's models, not just the Bodyguard.
- An `AttachedUnit` with zero attached units still counts as attached-sourced for ordering
  purposes (it displays identically to a plain `Unit`, but its source type is what the ordering
  rule keys on, not its rendered content).
- Equal-model-count units within a group break ties by ascending unit name (ordinal
  case-insensitive), so e.g. two "Crusader Squad with ..." blocks sort next to each other instead
  of wherever `Examples.View.MyArmy()` happened to place them.
- `AttachedUnitAggregateView` gains a new `IsAttachedUnit` field so the page layer can tell which
  source type produced a given view — the aggregate view currently has no way to distinguish
  them. Set in `AttachedUnitAggregator.Build` from `combatUnit is AttachedUnit`.
- The sort itself happens in `LivePlayModel.OnGet()` (page-layer concern), matching the existing
  precedent of weapon ordering being decided in `BuildUnitBlock` at the page layer rather than in
  domain code.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `live-play-view`: the "Live Play Page Lists Example Army Units" requirement currently states
  the page renders blocks "in the order returned" by `Examples.View.MyArmy()`. This is replaced
  with an explicit ordering rule: attached-sourced units first, then plain units, each group
  ordered by descending total model count.

## Impact

- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs` — new `IsAttachedUnit bool`
  field on the `AttachedUnitAggregateView` record.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — populate the new field.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` — sort `View.MyArmy()`'s output before mapping to
  `UnitBlockViewModel`.
- `openspec/specs/live-play-view/spec.md` — requirement text updated (delta spec in this change).
- Test coverage: `AttachedUnitAggregatorTests` (new field), a small ordering test at the page
  level (or a fixture-driven domain-level test if `LivePlayModel` isn't unit-tested today —
  confirm during design/tasks).
- No UI/markup changes — this is a render-order change only, not a new visual element.
