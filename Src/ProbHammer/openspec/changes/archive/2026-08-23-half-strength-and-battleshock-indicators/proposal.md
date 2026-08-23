## Why

`/LivePlay` tracks live casualties per model-line but has no way to surface two of 11th edition's
core consequences of those casualties: a unit that is at or below half its starting strength must
take a Battle-shock test each Command phase, and a Battle-shocked unit's Objective Control drops to
nothing until it passes one. Right now the player has to track both entirely by memory or on paper
even though the app already holds the exact data (`RemainingCount`/`Count` per model-line) that
determines half-strength for every multi-model unit.

## What Changes

- A new `IsAtOrBelowHalfStrength` computation for any `ICombatUnit`: combined starting strength is
  `Σ ModelLine.Count` across every `Components` entry (so an `AttachedUnit`'s Bodyguard and every
  Attached unit count together, per the real rule's "starting strength of an attached unit is the
  number of models it contains at the start of the first battle round"); the unit is at/below half
  when `CurrentStrength <= floor(StartingStrength / 2)`. Fully derived from existing
  `RemainingCount`/`Count` data — no new input for any unit whose combined starting strength is 2
  or more.
- A manual, per-unit, persisted "at half strength" override for the one case that isn't computable
  from model count: a combined starting strength of exactly 1 (a genuine single-model unit with no
  attachment), where the real rule is wound-based and this app deliberately does not track partial
  wounds (physical wound markers cover that at the table).
- A manual, per-unit, persisted "Battle-shocked" toggle — the 2D6-vs-Leadership roll happens at the
  table, never simulated by the app. Unlike casualty state, this flag never auto-clears; 11th
  edition only clears Battle-shock when the unit *passes* a subsequent test, so the player toggles
  it off themselves.
- `/LivePlay` unit-block header (`<h2 class="unit-name">`) gains two right-aligned glyphs, visible
  regardless of whether that unit's Statline/Ranged/Melee sections are expanded or collapsed: a `½`
  toggle (interactive only for the single-model manual-override case; a passive indicator for the
  computed case) and an always-present `⚡` Battle-shock toggle, rightmost.
- Every Objective Control stat-tile for a Battle-shocked unit renders in the same visual family as
  the existing caveated-Invulnerable-Save tile (an amber-tint background, signaling "this value
  isn't what it looks like"), with its value replaced by `⚡` rendered in the ordinary stat-value
  text color — never `--amber` — so the "this is a flagged value" signal lives entirely in the
  tile's background token, not in per-glyph color, keeping a future re-theme to a single token swap.
- Both new toggles persist across a page reload using the same client-side pattern as casualty
  marking: `localStorage` plus a full POST-and-server-rebuild sync, no new server-held state
  between requests.

## Capabilities

### New Capabilities
(none — this extends two existing capabilities)

### Modified Capabilities
- `attached-unit-tracker`: adds live, per-`ICombatUnit` Below/At-Half-Strength computation (and its
  single-model manual-override exception) and a persisted Battle-shocked flag, alongside the
  existing live casualty (`RemainingCount`) tracking this capability already owns.
- `live-play-view`: adds the unit-header `½`/`⚡` glyph rendering (including collapsed-section
  visibility), the Battle-shocked Objective Control tile rendering, and the persistence/sync
  behavior for both new toggles.

## Impact

- `ProbHammer.Core.Domain.Roster`: a new pure-function resolver (alongside `ToughnessResolution`,
  `KeywordResolution`) for half-strength computation; new live state fields for the manual
  half-strength override and Battle-shocked flag (mirroring where `ModelLine.RemainingCount` lives
  today).
- `ProbHammer.Web`: `_UnitBlock.cshtml` (header markup, OC tile rendering), `site.css` (header flex
  layout, glyph styling, a caveated-stat-tile class generalized off its current InSv-only naming),
  `live-play.js` (two new toggle controls following the existing casualty-control/localStorage/sync
  pattern), and whatever session/casualty-sync endpoint plumbing carries the two new flags through
  the existing rebuild-on-every-request flow.
- No change to any BSData ingestion, catalogue resolution, or army-list parsing code — this is
  entirely live-game-state and rendering, on top of data already resolved today.
