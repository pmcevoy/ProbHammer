## Why

`/LivePlay` was built with no responsive breakpoint at all (design-tokens.md: "No dedicated
responsive breakpoint today"). Testing against the two real target viewports (chrome-devtools-mcp
emulation, cross-checked against real iPhone screenshots — 375x539 portrait, 667x315 landscape)
found it fails differently in each orientation: in portrait, `.statline-tiles`' fixed 6-column
grid clips core game data (Leadership and Objective Control render entirely outside the visible
box on every unit) and a long weapon-keyword chip overflows into the numeric stat columns beside
it, producing garbled overlapping text; in landscape, the static header block (name bar + meta +
phase/turn tracker + an always-expanded Rules section) alone measures 321px against a 315px
viewport, so no unit is visible without scrolling past more than a full screen on first paint.

The app's core information layout — an ability rendered in its own column, row-aligned beside the
specific unit/model-line that grants it (`.statline-grid`'s `grid-row`-pinned columns) — is
inherently a wide-format design; collapsing it to single-column stacking for portrait would lose
the row alignment that layout exists to encode, not just look worse. Given the app's real audience
(a phone/tablet companion used tableside, never a laptop), the simpler and more honest fix is to
stop trying to support portrait on narrow devices at all, and put the effort into making
landscape's own vertical budget actually work.

## What Changes

- Removes orphaned CSS left over from the retired attacker/defender roster-builder and combat-
  simulator pages (`archive-10e-pipeline`): a confirmed contiguous 437-line dead block
  (`site.css` lines 64-500 — `.index-page`, `.catalogue-bar`, `.unit-card`, `.combat-panel`,
  `.mod-section-*`, `.sim-stats`, `.pipeline-table`, and everything between them), immediately
  preceding the comment header marking where the live `.live-play-page` CSS actually begins.
  Verified unreferenced by any class/element check across every `.cshtml`, `.cs` (including
  `ProbHammer.Core`'s one HTML-emitting renderer), and `.js` file the two live pages
  (`/LivePlay`, `/Import`) actually load. A handful of additional orphaned declarations chained
  onto selectors that may still be partly live (`abilities-cell`/`selected-attacker`/
  `weapon-type-locked` on the old unprefixed `.weapon-table`; `selected-defender` on `.unit-card`)
  need individual verification during implementation rather than blanket removal, since their base
  selectors aren't themselves confirmed fully dead.
- `/LivePlay` gates on orientation: on a narrow viewport (portrait, max-width ~600px) it shows a
  "rotate your device" prompt in place of the roster; a wide (tablet-class) portrait viewport, and
  `/Import` in any orientation, are unaffected — the gate only fires where the layout is actually
  known to break.
- The Army Rules section (Army/Detachment rule chips in `_ArmyHeader.cshtml`) defaults to
  collapsed, matching the existing "All Keywords" section's own disclosure convention, instead of
  always rendering expanded — the single largest contributor to the header's landscape height
  overflow.
- A whole unit block gains its own collapse control, beyond the existing per-section
  Statline/Ranged/Melee/Keywords disclosures, so a unit not currently relevant mid-game can be
  reduced to just its name bar.
- `.lp-section summary`'s section-title font size, and `.army-header-name` / `.unit-block
  .unit-name`'s font size (currently `1rem`, kept a step above the section-title size), are all
  reduced together, recovering vertical space on every unit card, not only the army header.
- `.statline-grid`'s separate Model Abilities and Unit Abilities columns are merged into one
  stacked-list column — revised mid-implementation, superseding an earlier fix that only collapsed
  the grid to 2 columns when a unit had no Model Abilities anywhere (confirmed to leave a ~163px
  dead strip between the statline card and the Unit Abilities column whenever that middle column
  was empty for a whole row-group). The user decided, after seeing the result, that the two scopes
  don't need their own separate columns at all; the domain-level Model/Unit distinction
  (`Ability.Scope`) is untouched, so a future per-ability scope marker remains an option.

## Capabilities

### New Capabilities
(none — this changes behavior on the existing `/LivePlay` page)

### Modified Capabilities
- `live-play-view`: adds an orientation-gate requirement (a narrow portrait viewport shows a
  rotate-device prompt instead of the roster; a wide portrait viewport and `/Import` are exempt);
  changes the Army Rules section's default disclosure state from always-expanded to collapsed;
  adds a new whole-unit-block collapse control alongside the existing per-section disclosures.

## Impact

- `src/ProbHammer.Web/wwwroot/css/site.css`: removes the confirmed dead legacy-page block (lines
  64-500) plus the handful of scattered orphaned declarations noted above; new orientation-gated
  media query and rotate-prompt overlay styling; `.statline-grid` column-allocation change (Model
  Abilities column no longer unconditionally reserved); reduced name-bar font sizes.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml`, `Pages/Shared/_ArmyHeader.cshtml`,
  `Pages/Shared/_UnitBlock.cshtml`: markup for the rotate-prompt overlay, the Army Rules section's
  default closed `<details>` state, and the new unit-block collapse control.
- `src/ProbHammer.Web/wwwroot/js/live-play.js`: wiring for the new unit-block collapse toggle,
  including state carry-forward on the existing casualty/status sync fragment swap (matching the
  established per-section disclosure carry-forward pattern from `live-play-phase-turn-tracker`).
- No change to `ProbHammer.Core` — this is presentation-layer only.
