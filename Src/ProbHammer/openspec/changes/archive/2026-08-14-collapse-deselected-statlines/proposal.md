## Why

The Statline section already tracks a per-statline/loadout selection state (added by
`live-play-selection-scoped-weapons`) that drives which weapons appear in the Ranged/Melee
sections. That same state currently has no visual effect on the Statline section itself: a fully
deselected statline (e.g. Helbrecht taken out of the current simulation, or one loadout-variant of
a squad) still renders its full stat-tile row and ability text, at full height, indistinguishable
from a statline that's actively in play. During a live game, deselection is used to declutter the
view around what's relevant right now, so the deselected rows should shrink out of the way instead
of continuing to take up screen space.

## What Changes

- A statline run's shared stat-tile collapses to just its entries' header lines (name + count each,
  no M/T/Sv/W/Ld/Oc tile) when **every** entry sharing that run — one, or several when the run
  merges multiple same-value statlines (e.g. Sword Brother and Initiate both Sv3+) — is fully
  deselected. It expands back to full content the moment any one entry in the run (any of its
  loadouts, or its own single toggle) is reselected.
- A Model Abilities or Unit Abilities column cell — whether it renders beside one run or, as a
  component-wide ability, visually spans every run belonging to its component — collapses only when
  **every** run within its span is fully deselected (i.e. every entry of every run in that span).
  It stays fully expanded as long as at least one run in its span has any selected or partial entry.
- Collapse/expand is purely visual (CSS-driven height/overflow transition on cell content, triggered
  by the existing `select-selected` / `select-deselected` / `select-partial` classes already toggled
  by `setState()` in `live-play.js`). No new selection state, no change to grid row indices, and no
  change to which weapons render in the Ranged/Melee sections.

## Capabilities

### Modified Capabilities
- `live-play-view`: the Statline section's "Statline Section Rendering" and "Statline Ability
  Column Rendering" requirements gain collapse behavior driven by the existing selection state.

## Impact

- `src/ProbHammer.Web/wwwroot/css/site.css` — collapsed-state rules for `col-statline`,
  `col-model-abilities`, and `col-unit-abilities`, including their `.spans-component` variants.
- `src/ProbHammer.Web/wwwroot/js/live-play.js` — `setState()` already applies the classes this
  change keys off of; likely needs to also derive and apply an aggregate collapsed/expanded class
  on each run's `col-statline`/`col-model-abilities` cells (collapse condition spans every entry in
  that run) and on `spans-component` cells (collapse condition spans every entry of every run in
  that component's ability span), neither of which map onto a single entry's own state.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml` — no markup restructuring expected; existing
  `statline-grid` DOM elements gain the hooks needed for the above.
- No change to `ProbHammer.Web/Pages/LivePlay.cshtml.cs`, the aggregation domain model, or the
  Ranged/Melee weapon sections.
