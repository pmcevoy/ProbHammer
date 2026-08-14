## Why

`/LivePlay` already computes and renders remaining-vs-initial model counts per statline
(`AttachedUnitAggregateView.Statlines`, `attached-unit-tracker`'s "Model-Line Remaining Count" and
"Aggregate Statline View" requirements), and the domain already supports removing casualties
(`ModelLine.RemoveCasualties`). But nothing on the page lets a player actually mark a casualty —
the page is still read-only, rebuilt fresh from `Examples.View.MyArmy()` on every request with no
persistence anywhere. A player at the table needs to record casualties as the game happens and see
that state survive page reloads (and a server restart) for the rest of a multi-hour game.

## What Changes

- Each statline entry (and, for a multi-loadout entry, each nested loadout line) in the Statline
  section gains a casualty control that decrements/increments its remaining count, clamped between
  0 and its initial count. Increments beyond removing a casualty are included deliberately — a
  player needs to correct a mis-tap without reloading the page.
- Casualty state is keyed per model-line using a stable identity derived from the same
  `(unit index, ComponentName, StatlineName, loadout index)` coordinate already used by
  `LivePlayModel.SelectKey` for client-side selection filtering (extended with a unit-level index,
  since that state is scoped to one unit block and casualty state spans the whole page) — this
  reuses an existing, proven convention rather than introducing a persisted id on `ModelLine`.
  This relies on `Examples.View.MyArmy()` and `LivePlayModel.OnGet()`'s ordering being deterministic
  across requests, which they already are (no randomness, fixed sort).
- The browser persists the casualty map (coordinate → remaining count) to `localStorage`, so it
  survives page reloads and browser restarts.
- A new endpoint accepts the casualty map, rebuilds the `ICombatUnit` tree from
  `Examples.View.MyArmy()`, applies each entry via `ModelLine`'s remaining-count adjustment, reruns
  `AttachedUnitAggregator`, and returns the updated view data. The client posts on page load (to
  apply whatever's in `localStorage` before first render) and after every casualty tap (no full page
  reload) — mirroring the existing `/api/simulate` pattern (`ArmyView`'s "Run Simulation" button
  posts and injects the response without reloading).
- `ModelLine` gains the ability to move its remaining count back up (bounded at its original
  `Count`), not just down — needed for the mis-tap correction above. `RemoveCasualties` alone is
  one-directional today.
- **A statline entry stays listed (at 0/N) once every one of its model-lines reaches 0 remaining,
  instead of disappearing from the aggregate view entirely.** `AttachedUnitAggregator.BuildStatlines`
  currently drops an entry outright once it's fully dead — with nothing rendered, there'd be no row
  left to carry the revert control this change adds. The fix is narrow: the entry is only ever
  omitted when it was *never* present in the roster at all (preserving the existing "never show
  unselected wargear" rule); once it has model-lines, it stays listed regardless of remaining count.
- **A statline run collapses to header-only once every entry in it is fully dead, in addition to
  the existing fully-deselected trigger** (`collapse-deselected-statlines`) — reusing that same
  visual treatment rather than introducing a new one. Unlike the deselection trigger, which is
  client-side and always resets, the dead trigger is computed from `RemainingCount` (known
  server-side) and baked into the initial render — so it's the default appearance after a reload,
  not something a player has to re-trigger. The two triggers are independent: reselecting a dead
  entry does not un-collapse it (only reverting the casualty does); a live entry's deselection
  collapse behaves exactly as it does today.
- **Confirms (and adds explicit spec coverage for) already-shipped behavior**: a model-line or
  loadout at 0 remaining contributes nothing to any weapon's contribution breakdown —
  `AttachedUnitAggregator.BuildWeapons` already only ever consumes `RemainingCount > 0` lines, so a
  dead loadout was never going to render as a 0×-attacks row. This change is simply the first thing
  that actually exercises that path via live mutation, so it gets a scenario locking it in.
- Out of scope: undo history/audit trail beyond the current remaining count, and multi-device sync
  (casualty state is local to the browser that recorded it).

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `live-play-view`: adds a casualty-tracking control per statline entry/loadout, the
  page-load/post-tap sync flow against the new endpoint, `localStorage` as the persistence layer
  for casualty state across reloads, and extends the existing run-collapse mechanism
  ("Statline Section Rendering", "Statline Ability Column Rendering") with a second,
  server-rendered "fully dead" trigger alongside the existing client-side "fully deselected" one.
- `attached-unit-tracker`: "Model-Line Remaining Count" becomes two-directional (down for a
  casualty, back up to correct a mis-tap, clamped to `[0, Count]`) instead of only decrementing.
  "Aggregate Statline View" no longer drops an entry once it's fully dead — it stays listed at
  0/N as long as it was ever present. "Aggregate Weapon Count View" gains an explicit scenario
  confirming a dead model-line/loadout is excluded from a weapon's contributions entirely (already
  true in the shipped implementation).

## Impact

- **Changed (domain)**: `src/ProbHammer.Core/Domain/Roster/ModelLine.cs` gains a bounded
  remaining-count adjustment in both directions.
- **Changed (page)**: `src/ProbHammer.Web/Pages/LivePlay.cshtml` (casualty controls, coordinate
  `data-*` attributes), `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` (rebuilding the roster from a
  posted casualty map instead of always using pristine `Examples.View.MyArmy()`),
  `src/ProbHammer.Web/wwwroot/js/live-play.js` (localStorage read/write, POST-on-load and
  POST-on-tap, DOM patching of the response — this page's first persisted client state).
- **New**: a server endpoint (exact route/shape decided in design.md) that accepts a casualty map
  and returns updated view data for the affected unit(s).
- **Changed (domain, aggregation)**: `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs`
  — `BuildStatlines`' drop-when-dead guard narrows to drop-when-never-present only.
- **Unchanged**: `AttachedUnitAggregator.BuildWeapons`/`BuildAbilities` (already correctly exclude
  dead lines — no code change, only added spec coverage), `Datasheet`/`Unit`/`AttachedUnit`
  construction, `Simulation/*`, `ArmyView`/`/api/simulate` (no shared code path — see
  `wh40k-11e-rewrite-scope` decision that `/LivePlay` doesn't need to match those pages'
  patterns).
- **Tests**: `tests/ProbHammer.Tests` gains cases for `ModelLine`'s two-directional remaining-count
  adjustment (clamped at both bounds) and for the new endpoint's rebuild-and-reapply behavior.
  Visual/interaction verification via `firefox-devtools-mcp` against the real example army:
  marking a casualty, reloading the page (state persists), correcting a mis-tap, and confirming the
  Statline/Weapon/Ability sections recompute exactly as `attached-unit-tracker`'s existing rules
  already specify.
