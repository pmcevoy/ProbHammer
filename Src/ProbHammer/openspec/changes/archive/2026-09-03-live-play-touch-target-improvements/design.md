## Context

See `proposal.md` - Why. This touches four independent areas of `/LivePlay`'s markup/CSS/JS
(`_UnitBlock.cshtml`, `_ArmyHeader.cshtml`, `_PhaseTurnTracker.cshtml`, `site.css`,
`live-play.js`), all in service of the same goal: make the page easier to use one-handed on a
phone. None of the four require a new server endpoint or a data-model change — every piece is
either pure CSS, a markup restructuring, or a client-side JS change that already has the
information it needs at its existing call site.

## Goals / Non-Goals

**Goals:**
- Grow tap targets that are currently smaller than their visible affordance (toolbar items,
  casualty +/- buttons) without changing what they do or how they look at rest.
- Let a player collapse things they don't need to see right now — either all units at once (via
  the phase/turn tracker's own row-label cells, with a phase column doing the inverse) or the
  entire Army Header — without losing any live state (casualties, statuses, selection filters,
  per-section disclosure).
- Rebalance the weapon tables' column widths to give the weapon name/tags column more room.

**Non-Goals:**
- No change to the statline selection indicator (explored, explicitly dropped - see proposal.md).
- No persistence of the army header's or a phase/turn-triggered unit-block collapse across a full
  page reload — neither is specified to persist today (`Unit Block Full Collapse` only guarantees
  survival across an *in-page* re-render, not a reload), and this change doesn't add that.
- No change to what data each weapon entry shows, or to the weapon-table header row structure
  itself (explored as a summary-bar merge, explicitly dropped after review - see proposal.md) -
  only the column widths shift.

## Decisions

### Decision 1: Toolbar tap target grows by moving the label inside the button
`_UnitBlock.cshtml`'s `.unit-toolbar-item` currently renders `<button class="unit-toolbar-icon-btn">`
(the glyph) as a sibling of a plain `<span class="unit-toolbar-label">` (the caption), so only the
small square button responds to a tap. Moving the label to be a child of the button (icon element
+ label span, both inside one `<button>`) makes the whole visible item clickable with no DOM
restructuring beyond that move - `.unit-toolbar-item`'s own padding already defines the desired hit
area. CSS changes to `.unit-toolbar-icon-btn` (fixed 1.4rem square → auto width, flex row containing
icon + label) and `.unit-toolbar-label` (loses its standalone-span styling, becomes an inline child
style). No JS changes: `live-play.js` selects these buttons by their semantic classes
(`.status-glyph-halfstrength`, `.status-glyph-battleshock`, `.casualty-reset-btn`), never by
`.unit-toolbar-icon-btn`, and reads `data-unit-index` off the same `<button>` element either way.

Alternative considered: keep the two elements as siblings and grow only the icon button's own
hit-slop via CSS (e.g. a larger invisible padding/`::before` hit area). Rejected - the label would
still visibly sit outside the "clickable-looking" glyph, which is exactly the affordance mismatch
being fixed; moving the label inside the button fixes both the actual hit area and its visual
legibility as one control.

### Decision 2: Phase/turn-driven unit-block collapse is a tri-state client-side override, no server change
`_PhaseTurnTracker.cshtml`'s click handler already calls `syncPhaseTurn(cell.dataset.turn,
cell.dataset.phase ?? null)` - a row-label cell has no `data-phase` attribute, so `phase` is `null`
at exactly this call site whenever a row label (not a phase column) was clicked, and non-null for
every phase column. That's exactly the condition the requirement needs ("Phase/Turn Selection Also
Drives Unit Block Collapse"), so no new field on `LivePlaySyncResponse` and no change to
`LivePlayCasualtyService`/`LivePlayModel` are needed - which selection kind this is is already
known client-side before the fetch is even sent.

First implementation only forced unit blocks *closed* on a row label and left them untouched on a
phase column (a plain boolean). Hands-on testing against the real intended flow - collapse
everything via a row label, work a phase, collapse again, work another phase - showed this was
wrong: a phase column's own Forced/Expanded inner sections (e.g. Shooting forcing Ranged Weapons
open) were computed correctly server-side but stayed invisible, hidden inside a unit block that a
prior row-label click had left collapsed. The fix generalizes the override to a tri-state value
covering both directions: `syncPhaseTurn` computes `const unitBlocksOpen = phase !== null;` (`true`
for any phase column, `false` for a row label) and threads it through
`applySyncResponse(fragments, forcedSections, unitBlocksOpen)` into `swapUnitBlock(unitIndex, html,
forcedSections, unitBlocksOpen)`. `swapUnitBlock`'s carry-forward line becomes `newEl.open =
unitBlocksOpen === null ? wasOpen : unitBlocksOpen;` - `null` (the default) preserves today's
unconditional carry-forward for the unrelated casualty/status-only sync path
(`syncLivePlayState`, which never passes this argument), while `true`/`false` fully overrides every
unit block on every phase/turn selection, including reselecting the cell already active.

This corrects `proposal.md`'s original Impact section, which speculated a server-side signal might
be needed before this design pass looked at `live-play.js` directly - it isn't, in either the
original or the revised design.

Alternative considered: report an `isRowLabelSelection` flag on `LivePlaySyncResponse` so the
server is the single source of truth for "is this a row label." Rejected as unnecessary
round-tripping - the client already has this fact for free, and `ForcedSections` already carries
the *inner*-section consequence of a phase/turn selection from the server; duplicating the
selection-kind itself server-side would be a second encoding of the same input with no behavioral
benefit.

### Decision 3: Army header collapse mirrors Unit Block Full Collapse exactly; the phase tracker moves out of the partial entirely
First implementation nested a second `<details>` inside `.army-header` to wrap just the meta line
and Rules section, keeping the phase tracker inside `.army-header` itself but outside that inner
wrapper. Direct user review rejected this on sight: two visually near-identical disclosure bars
("▼ Details" immediately above "▼ Rules") read as redundant, confusing chrome, and it didn't
actually solve the real want - "collapse this whole section so I can see my unit list," not
"collapse just the detail sub-panel while something else stays."

Revised structure, agreed directly with the user: `_ArmyHeader.cshtml`'s outer element becomes a
`<details class="army-header">` whose `<summary>` wraps the existing `<h1 class="army-header-name">`
- the exact same whole-block-collapse pattern `_UnitBlock.cshtml`'s own outer `<details>` already
uses (name bar as summary, collapsing hides everything else in one action). The phase/turn tracker
is removed from `_ArmyHeader.cshtml`/`ArmyHeaderRenderModel` entirely and rendered directly by
`LivePlay.cshtml`, as its own element between the Army Header partial and the unit-block loop -
this makes "the tracker is unaffected by the header's collapse" a structural fact (it's not even a
descendant of the collapsing element) rather than something requiring a protective wrapper inside
the partial. `ArmyHeaderRenderModel` drops its now-unused `PhaseTurn` field; `LivePlay.cshtml`
passes `Model.PhaseTurn` (already available there) straight to `_PhaseTurnTracker` itself. The
tracker gets its own card styling (`background`/`border`/`border-radius` matching `.army-header`
and `.unit-block`) since it's now a peer element on the page rather than relying on `.army-header`'s
own surface to read as "part of a card."

CSS mechanism: `<summary>` is given `display: contents` so it contributes no box of its own (its
native click-to-toggle behavior survives regardless - confirmed live, including via the
accessibility tree reporting a proper `DisclosureTriangle` role - since the toggle is bound to the
`<summary>` element itself, not to whatever box it renders), letting `.army-header-name` (the `<h1>`
inside it) become the effective, directly-styled bar child - same visual result as
`_UnitBlock.cshtml`'s `.unit-name` being the `<summary>` itself, but preserving the army name's own
heading semantics (a bare `<summary>` can't itself be an `<h1>`). This is verified, not merely
assumed: a live click on the rendered `.army-header-name` element toggles `.army-header.open`
correctly, and the accessibility snapshot lists it as an `expandable`/`expanded` disclosure.

Rules and All Keywords stay direct children of `.army-header`, same as before this whole rework -
each keeps its own independent `open` attribute, so collapsing/re-expanding the outer header leaves
their own state untouched (native nested-`<details>` behavior, unchanged from the original design's
own reasoning on this point).

Alternatives considered and rejected: the original nested-"Details"-wrapper design above (rejected
by direct user review); a single flat `<details>` spanning the whole `.army-header` with
`::before`/absolute positioning to pin the phase tracker outside the collapsing flow (rejected even
before user review - fighting a native `<details>`'s own show/hide of its children via CSS tricks
is fragile, and moving the tracker out of the partial entirely is simpler and now the agreed
approach anyway).

### Decision 4: Weapon-table column widths are rebalanced, header row structure is untouched
The `<thead>`/`<th>` row and its column labels (Rng, A, BS/WS, S, AP, D) stay exactly as they are
today - a merge into the section's own `<summary>` bar was explored and rejected (see the
Alternative below), and a "fused header" visual-only variant that would have kept the two elements
separate but made them read as one was considered and also not pursued, so this item is scoped down
to column-width rebalancing alone. `.col-slot`'s reserved width (currently a flat 11% each × 5-6
slot columns) shrinks, and the difference is handed to `.col-weapon`/`.col-weapon-melee`; `<th>`/
`<td>` horizontal padding may also tighten slightly. Exact values are an implementation/tasks-level
detail, verified on a real 375px-viewport screenshot per `.claude/implementation-notes.md`'s Mobile
Viewport Emulation section, not a design-level decision. Because the labels and their column stay
inside one `<table>` with its existing `<colgroup>`, header/body width alignment remains automatic
- there is no new alignment-fragility risk to manage here.

Alternative considered (rejected): move the `<thead>` column labels into that section's own
`<summary>` disclosure bar, eliminating the `<thead>` row entirely. Rejected on review for three
reasons: it conflates a static data label with `.lp-section summary`'s own interactive
toggle-control affordance; the six short labels plus the existing title and conditional "filtered"
badge may not fit on one line at 375px width, and if they don't, the "row eliminated" saving
evaporates (the summary just grows a second line instead); and it would require hand-maintaining
matching width fractions in two independent layout contexts (the summary's own flex/grid row vs.
the table's `<colgroup>`) instead of getting that alignment for free from one shared `<table>`. A
"fused" middle ground (keep `<thead>` as its own row, but make it visually continuous with the
summary bar - same background, no gap, no radius break) was also raised as a way to kill the
redundant-chrome feel without the alignment/affordance costs above, but wasn't pursued either -
out of scope for this change.

### Decision 5: Keyword filter forces the unit block open too, and scrolls to the first match on activation
Found while testing Decision 2's unit-block collapse: `applyKeywordHighlighting()` already forced a
matching unit's Keywords section open on an active filter, but never the unit block containing it -
a pre-existing gap (this function predates unit-block collapse entirely), invisible until this
change made bulk-collapsing every block a one-tap action. Fix mirrors Decision 2's own pattern
exactly: when a unit's Keywords section is forced open, also set `unitEl.open = true` alongside it.
The existing "one-way ratchet" rule (never force anything *closed*) is preserved - a non-matching
unit's block is left exactly as it was, in either direction.

The scroll-to-first-match behavior is a distinct, additive feature (not a bug fix), added right
after per direct user request, anticipating the same "collapse via My Turn, then pick a keyword"
navigation flow the earlier fix was tested against. Implemented in the keyword chip's own click
handler (`renderArmyKeywordChips`), not inside `applyKeywordHighlighting()` itself, specifically
because that function also runs from unrelated re-render call sites (a casualty/status sync, a
phase/turn sync, or re-rendering the chip list after a *different* keyword's click) -
scroll-on-every-highlight-pass would cause a surprise scroll-jump on those unrelated updates. A
local `activating` flag (computed from Set membership before mutating it) gates the scroll to
activation only, matching the requirement that deactivating a filter never scrolls.

## Risks / Trade-offs

- **[Risk] Growing `.unit-toolbar-icon-btn`'s hit area changes its layout math** (it's currently a
  fixed 1.4rem square placed by `.unit-toolbar-item`'s flex row) → **Mitigation**: verify on a real
  375px/667px viewport screenshot (per `.claude/implementation-notes.md`) that three toolbar items
  plus their dividers still fit on one row without wrapping, since Half Strength/Battle-shock/Reset
  Casualties' labels are the widest text in that row.
- **[Risk] A player who used "My Turn"/"Their Turn" purely as a quick "close everything" shortcut,
  without wanting the unit-block-level collapse, now gets a bigger visual jump than before** →
  **Mitigation**: none needed structurally (this is the explicitly requested behavior); superseded
  in practice by Decision 2's revision, which makes the collapse/expand cycle a core, intended part
  of the tracker's own workflow rather than an incidental side effect.

## Open Questions

None. Two design points were revised after direct, hands-on review against the running page rather
than left open: whether a phase-column selection should leave unit-block state untouched or force
it open (Decision 2 - resolved to "force open," confirmed against the user's own described usage
flow), and whether the Army Header's collapse should wrap only its meta/Rules content or the whole
header including the phase tracker's placement (Decision 3 - resolved to "whole header collapses;
tracker moves out of the partial entirely," per direct user feedback that the first version's
nested double-disclosure-bar looked redundant).
