## Why

`/LivePlay` always shows a unit's Ranged/Melee weapon totals summed across every one of its
contributing `ModelLine`s. Mid-game a player often only cares about a subset — e.g. only the models
still relevant to one specific ongoing fight when an Attached Unit's components have been split
across two combats. There's currently no way to see what a subset of a unit would contribute
without doing that arithmetic by hand.

## What Changes

- Every statline-header line and, where present, every nested loadout line in the Statline section
  becomes a tap/click target that toggles whether that statline/loadout is currently "selected."
  Everything is selected by default (today's always-everything behavior, unchanged until the player
  interacts). A statline with more than one loadout is tri-state — fully selected, fully
  deselected, or partial (some but not all loadouts selected) — with a partial state rendered as a
  distinct visual indicator. Clicking a loadout toggles just that loadout; clicking its parent
  statline header selects all its loadouts if the statline is currently anything other than fully
  selected, or deselects all of them if it's currently fully selected.
- The Statline section's `<summary>` shows a live "N/M selected" badge whenever the selection isn't
  "everything." The Ranged Weapons and Melee Weapons section summaries each independently show a
  "filtered" badge whenever the current selection would hide or resize at least one row in that
  specific section — so a section entirely unaffected by the current filter shows no badge.
- A weapon row (one row in the Ranged or Melee table) is **hidden entirely** when none of its
  contributions belong to a currently-selected statline/loadout. When some but not all of its
  contributions are selected, the row stays visible with its Attacks total recomputed from only the
  selected contributions.
- **Extends the already-shipped weapon-contribution-breakdown merge rule** (`add-live-play-weapon-
  provenance`): a breakdown group today merges into one row whenever every contribution in that
  `(ComponentName, StatlineName)` group shares the same `PerModelAttacks`. This adds selection
  agreement to that same condition — a group whose members currently agree (all selected or all
  deselected) still merges exactly as it does today; a group whose members disagree splits into one
  row per loadout, each labeled `"{StatlineName} w/ {compressed per-loadout label}"` (e.g. `"Initiate
  w/ Astartes chainsword"`) rather than the bare compressed label alone (`compress-loadout-labels`'
  compressed label reads fine in the Statline section because it's visually nested under its
  statline's own header line; a weapon breakdown row has no such nesting, so the bare label alone
  would misread as if the weapon itself were the contributor), showing only the still-selected
  member(s) — a deselected sibling's row disappears from the breakdown rather than rendering struck
  through, matching how a fully-excluded weapon's row disappears outright. Worked example against
  the real Crusader Squad fixture: with nothing filtered, Bolt Pistol still reads as one merged
  `"Initiate (5×1)"` row inside a total of 10 (Neophyte 4 + Initiate 5 + Crusade Ancient 1), exactly
  as today; deselecting only the Power-fist-armed Initiate loadout (2 of those 5 models) drops the
  total to 8 and splits the breakdown to show `"Neophyte (4×1)"`, `"Initiate w/ Astartes chainsword
  (3×1)"`, and `"Crusade Ancient (1×1)"` — the Power-fist loadout's row is
  simply absent.
- An explicit "Clear filter" control resets every statline/loadout back to fully selected. Visible
  only while a filter is currently active.
- All filtering and recomputation happens **client-side**: the data needed (per-contribution
  `Count`/`PerModelAttacks`, extended with a loadout identity — see Impact) already ships in the DOM
  via the existing contribution-breakdown rows. No new server endpoint or round-trip.
- Selection state is ephemeral, client-side-only, and does not persist across page reloads —
  consistent with `/LivePlay`'s existing architecture (rebuilt fresh from `Examples.View.MyArmy()`
  per request, no session/persistence anywhere else on this page either).
- **Depends on `compress-loadout-labels` being implemented and archived first** — this change's
  split contribution-breakdown rows render using that change's compressed per-loadout labels, and
  this proposal's spec deltas below assume that change's `Statline Section Rendering` requirement
  (the compressed-label version) is already the current baseline.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `live-play-view`: "Statline Section Rendering" gains tri-state statline/loadout selection and its
  live selected-count badge. "Weapon Section Rendering" gains selection-scoped hide/recompute
  behavior for weapon rows, an extended contribution-breakdown merge/split rule keyed on selection
  agreement in addition to `PerModelAttacks`, section-level "filtered" badges, and the Clear-filter
  control.

## Impact

- **Changed (domain)**: `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` and
  `WeaponContribution` gain an explicit loadout identity (a stable ordinal matching the contributing
  `ModelLine`'s position within its `AggregateStatlineEntry.Loadouts` list) — needed so client-side
  filtering can correlate a specific contribution to a specific loadout selection unambiguously;
  today two sibling loadouts under the same statline name are indistinguishable in a
  `WeaponContribution` beyond their `Count`, which is not reliable (two loadouts can coincidentally
  share a model count).
- **Changed (page)**: `src/ProbHammer.Web/Pages/LivePlay.cshtml` (selection UI markup, badges, Clear
  filter control, `data-*` attributes carrying loadout identity for JS to key off),
  `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` (threading the new loadout identity into the
  view-model), `src/ProbHammer.Web/wwwroot/css/site.css` (selection state styling).
- **Changed (JS)**: `src/ProbHammer.Web/wwwroot/js/live-play.js` grows substantially beyond its
  current single-purpose toggle script — this is the page's first client-side state (selection)
  and first client-side arithmetic (recomputed Attacks totals).
- **Unchanged**: `Datasheet`, `Unit`, `AttachedUnit`, `DiceExpression`, `Simulation/*`, server-side
  aggregation math in `AttachedUnitAggregator.BuildWeapons` (still computes full, unfiltered
  totals — filtering is a client-side view concern only).
- **Tests**: `tests/ProbHammer.Tests` gains cases for the new loadout identity field and the
  extended merge/split rule (agreeing vs. disagreeing selection state) against the real Crusader
  Squad fixture. Visual verification via `firefox-devtools-mcp` against the real example army,
  exercising default rendering, whole-statline exclusion, single-loadout exclusion with recompute
  and breakdown split, Clear filter, and badge appearance/disappearance across all three sections.
