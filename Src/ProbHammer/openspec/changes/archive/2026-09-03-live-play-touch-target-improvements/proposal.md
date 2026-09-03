## Why

`/LivePlay` is used one-handed on a phone at the table, and several of its controls don't hold up
under that use: the unit status toolbar's tap target is smaller than its visible caption suggests,
the casualty +/- buttons sit close enough together to mis-tap the wrong direction, the always-visible
army header spends space that isn't always needed, and the weapon tables' name/tags column is
tighter than it needs to be. This is a small, batched usability pass addressing all of it in one
review cycle rather than one-off patches.

## What Changes

- **Unit status toolbar tap target**: the entire `.unit-toolbar-item` (icon *and* its caption
  label) becomes the activatable region for Half Strength / Battle-shock / Reset Casualties, not
  only the small icon glyph — matching the "the whole bar is the trigger" convention already used
  by `.lp-section summary` and `.phase-turn-cell` elsewhere on this page. Visual layout of icon +
  label is unchanged; only the click/tap boundary grows.
- **Casualty +/- controls sized for touch**: `.casualty-btn` (mark/undo a casualty) grows and the
  gap between the two buttons in a `.casualty-controls` pair increases, reducing the chance of
  tapping the wrong direction. Implementation/CSS only — no behavioral change (still disabled at 0
  and at the initial count, same actions).
- **Every phase/turn selection also drives unit-block collapse**: selecting a "My Turn"/"Their
  Turn" row-label cell collapses every unit block on the page to its name bar (a compact,
  low-scroll roster list); selecting a specific phase column instead expands every unit block, so
  that phase's own Forced/Expanded inner sections (Statline, Ranged Weapons, etc.) are actually
  visible rather than forced open server-side yet hidden inside a still-collapsed block. This is a
  full override applied on every selection (including reselecting the cell already active), not a
  one-time transition — confirmed against the intended real usage flow: collapse everything via "My
  Turn", work through Movement (blocks expand, Statline visible), hit "My Turn" again to collapse,
  then Shooting (blocks expand, Ranged Weapons now visible too). Revised from an earlier draft that
  left unit-block state untouched by a phase-column selection — hands-on testing showed that left
  the existing per-phase section-forcing invisible whenever blocks were already collapsed.
- **Army Header becomes independently collapsible**: the whole header collapses to its own name
  bar in one action — the same `Unit Block Full Collapse` mechanism unit blocks already use,
  applied to the header itself — so a player can get straight to the unit list without its
  meta/Rules detail in the way. The phase/turn tracker is no longer part of the Army Header at all;
  it now renders as its own element between the header and the first unit block, so it's
  structurally unaffected by the header's collapse rather than needing special-casing to protect
  it. This collapse is independent of, and never driven by, the row-label unit-block collapse above
  — each is its own, separately-set piece of state.
- **Weapon table column spacing tightened**: the Ranged/Melee Weapons tables' numeric
  characteristic columns (Rng, A, BS/WS, S, AP, D) give up some of their reserved width, handed to
  the weapon name/tags column instead. Implementation/CSS only — the `<thead>` row, its labels, and
  every value the existing spec requires per weapon entry are unchanged; only the column widths
  shift.
- **Keyword filter also expands the matching unit block, not just its Keywords section**: found
  while testing the row-label collapse above — activating a keyword in the "All Keywords" section
  already forced a matching unit's Keywords section open, but never the unit block containing it,
  so the match was invisible whenever that block was collapsed (e.g. via "My Turn"). Same class of
  fix as the phase/turn one: the forced-open descendant needs its ancestor forced open too. A
  pre-existing gap in already-shipped behavior, not something this change introduced — just not
  reachable in practice until this change made bulk-collapsing every block trivial.
- **Activating a keyword filter scrolls to its first match**: speeds up the intended "collapse
  everything, then jump to a keyword" navigation flow — activating a keyword's filter scrolls the
  first matching unit block (in page order) to the top of the viewport, already expanded. Scoped to
  activation only (never deactivation) and to the specific keyword just clicked, and never
  triggered by an unrelated re-render (a casualty adjustment, a phase/turn selection, or activating
  a *different* keyword) re-evaluating the same active-filter state.
- Dropped from scope after discussion:
  - Restyling the statline selection indicator away from its checkbox look. Kept as-is — its
    tri-state "partial" glyph (a multi-loadout statline with only some loadouts selected)
    communicates something a simple grey-out can't.
  - Merging the weapon-table's `<thead>` column labels into the section's own `<summary>` bar.
    Reconsidered after review: it would conflate a static label with an interactive toggle control,
    risked not fitting on one line at 375px width, and would break the automatic header/body
    column-width alignment a single `<table>` + `<colgroup>` gives for free today. A fused-header
    visual alternative (keep two elements, make them read as one) was also considered and not
    pursued.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `live-play-view`: the "Unit Status Toolbar" requirement gains an explicit tap-target rule (the
  whole toolbar item, not only its icon, is the activatable region). The "Keyword Filter Expands
  And Highlights Matching Units" requirement gains the unit-block-forcing behavior above. New "Army
  Header Full Collapse" (mirroring "Unit Block Full Collapse" for the header itself) and "Keyword
  Filter Scrolls To First Match" requirements are added; the existing "Army Header Rendering"
  requirement is unchanged (what renders when expanded is untouched).
- `live-play-phase-tracker`: the "Phase/Turn Selector Control" requirement's positioning claim is
  corrected — it no longer renders "within the Army Header" (that was true before this change, and
  the delta drafting this rework missed updating it) but between the Army Header and the first unit
  block, outside the Army Header entirely. A new requirement makes every phase/turn selection fully
  set every unit block's collapsed/expanded state (per `live-play-view`'s "Unit Block Full
  Collapse") — a row label collapses all, a phase column expands all — as a standing override on
  every selection, not a one-time transition.

## Impact

- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — toolbar markup, casualty control markup
- `src/ProbHammer.Web/Pages/Shared/_ArmyHeader.cshtml` — the whole element becomes a `<details>`
  (name bar as `<summary>`), phase tracker removed from this partial entirely
- `src/ProbHammer.Web/Pages/LivePlay.cshtml` / `LivePlay.cshtml.cs` — phase/turn tracker now
  rendered directly here, between the Army Header partial and the unit-block loop;
  `ArmyHeaderRenderModel` drops its `PhaseTurn` field (no longer needed by that partial)
- `src/ProbHammer.Web/wwwroot/css/site.css` — toolbar hit-target sizing, casualty button
  size/spacing, weapon-table column-width spacing, army-header disclosure styling, phase-turn
  tracker's own card styling (now a peer of the Army Header rather than nested inside it)
- `src/ProbHammer.Web/wwwroot/js/live-play.js` — every phase/turn sync now passes a tri-state
  `unitBlocksOpen` override (`null` for the unrelated casualty/status-only sync path, `true`/`false`
  for every phase/turn selection) through `applySyncResponse` into `swapUnitBlock`, replacing that
  function's previous unconditional per-block carry-forward for this one case. Resolved entirely
  client-side (see design.md Decision 2) — no server contract change: the existing
  `syncPhaseTurn(turn, phase)` call site already knows whether `phase` is `null` (a row label) or
  not (a phase column) before the fetch is even sent. `applyKeywordHighlighting` now also forces a
  matching unit's own block open (not only its Keywords section); its chip click handler
  additionally scrolls to the first matching unit on activation only, via
  `Element.scrollIntoView`.
