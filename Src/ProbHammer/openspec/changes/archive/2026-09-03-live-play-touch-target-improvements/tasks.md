## 1. Unit status toolbar tap target

- [x] 1.1 In `_UnitBlock.cshtml`, move each `.unit-toolbar-label` `<span>` inside its
      `.unit-toolbar-icon-btn` `<button>` (icon element + label, both children of one button) for
      all three controls (Half Strength, Battle-shock, Reset Casualties)
- [x] 1.2 Update `.unit-toolbar-icon-btn` CSS: from a fixed 1.4rem square to an auto-width flex row
      containing the icon and label, preserving today's actionable/inert (opacity) and
      active/inactive (glyph color) visual states unchanged
- [x] 1.3 Update/remove `.unit-toolbar-label`'s standalone styling now that it's an inline child of
      the button rather than a sibling span
- [x] 1.4 Confirm `live-play.js`'s existing selectors (`.status-glyph-halfstrength`,
      `.status-glyph-battleshock`, `.casualty-reset-btn`) and `data-unit-index` still resolve
      correctly against the restructured button (no JS change expected, per design.md Decision 1)
- [x] 1.5 Verify on a real 375px/667px viewport screenshot (`.claude/implementation-notes.md`
      Mobile Viewport Emulation) that all three toolbar items plus their dividers still fit on one
      row without wrapping. Verified via `chrome-devtools` at 667px landscape: all three items
      share the same top offset (no wrap), using 435px of the 633px available toolbar width.

## 2. Casualty +/- control sizing

- [x] 2.1 Increase `.casualty-btn`'s width/height beyond today's 1.4rem
- [x] 2.2 Increase `.casualty-controls`'s gap beyond today's 0.25rem
- [x] 2.3 Verify on a real 375px-viewport screenshot that a multi-loadout statline row (name +
      count + select-indicator + the wider casualty controls) doesn't force awkward wrapping.
      Verified via `chrome-devtools` screenshot at 667px landscape - loadout rows render on one
      line with clean spacing between label and controls.

## 3. Phase/turn selection drives unit-block collapse (revised - see design.md Decision 2)

- [x] 3.1 In `live-play.js`'s `syncPhaseTurn(turn, phase)`, compute
      `const unitBlocksOpen = phase !== null;` (`true` for any phase column, `false` for a row
      label)
- [x] 3.2 Thread `unitBlocksOpen` through `applySyncResponse` into `swapUnitBlock` as a tri-state
      value (`null` default for the unrelated casualty/status-only sync path)
- [x] 3.3 In `swapUnitBlock`, change `newEl.open = wasOpen;` to
      `newEl.open = unitBlocksOpen === null ? wasOpen : unitBlocksOpen;`
- [x] 3.4 Confirm `syncLivePlayState`'s casualty/status-only call path never passes `unitBlocksOpen`
      (defaults to `null`), preserving today's unconditional carry-forward for that path
- [x] 3.5 Manually verify the full intended flow: "My Turn" collapses every unit block; "Movement"
      expands every block with Statline forced open; "My Turn" again re-collapses every block;
      "Shooting" expands every block with Statline *and* Ranged Weapons forced open. Verified live
      via `chrome-devtools` against a real import - confirmed exactly this sequence.

## 4. Army header collapse (revised - see design.md Decision 3)

- [x] 4.1 In `_ArmyHeader.cshtml`, make the whole element a `<details class="army-header" open>`
      with a `<summary>` wrapping the existing `<h1 class="army-header-name">` - the same
      whole-block-collapse pattern `_UnitBlock.cshtml`'s own outer `<details>` already uses. Remove
      the `_PhaseTurnTracker` partial from this file entirely.
- [x] 4.2 In `LivePlay.cshtml`, render `_PhaseTurnTracker` directly (model `Model.PhaseTurn`),
      between the `_ArmyHeader` partial and the unit-block loop, so it's structurally outside the
      collapsing element rather than protected by an in-partial wrapper
- [x] 4.3 Remove the now-unused `PhaseTurn` field from `ArmyHeaderRenderModel`
      (`LivePlay.cshtml.cs`) and update its doc comment; update the `_ArmyHeader` partial's model
      construction in `LivePlay.cshtml` accordingly
- [x] 4.4 CSS: give `.army-header > summary` `display: contents` so it contributes no box of its
      own (confirm its native click-to-toggle behavior still works - it does, since the toggle is
      bound to the `<summary>` element itself, not its rendered box), and move the disclosure arrow
      (`::before`, rotate-on-`[open]`) onto `.army-header-name` directly, mirroring
      `.unit-block .unit-name`'s own arrow treatment
- [x] 4.5 Give `.phase-turn-tracker` its own card styling (`background`/`border`/`border-radius`
      matching `.army-header`/`.unit-block`) since it's now a peer element on the page rather than
      relying on `.army-header`'s own surface
- [x] 4.6 Remove the obsolete nested-wrapper CSS from the rejected first draft
      (`.army-header-details` and its child-margin rule)
- [x] 4.7 Manually verify: clicking the header's name bar collapses the whole header (meta line,
      Rules, All Keywords all hidden, only the name bar remains); the phase/turn tracker below it
      stays visible and functional throughout; re-expanding restores the Rules section's own prior
      open/closed state; collapsing/expanding the header never changes any unit block's own
      collapsed state, and vice versa. Verified live via `chrome-devtools`, including the
      accessibility tree reporting a proper `DisclosureTriangle` role.

## 5. Weapon table column-width rebalancing

- [x] 5.1 Reduce `.col-slot`'s reserved width share (currently a flat 11% per slot column) and
      reallocate the difference to `.col-weapon`/`.col-weapon-melee` in `site.css`
- [x] 5.2 Tighten characteristic-column horizontal padding (`.weapon-table th`/`td`) to match,
      keeping the existing `<thead>` row and its labels unchanged
- [x] 5.3 Verify on a real 375px-viewport screenshot that weapon names/tags have visibly more room
      and that Ranged vs. Melee tables' column widths still align with each other where they share
      the same columns (A, S, AP, D). Verified via `chrome-devtools`: the "A" column's left edge is
      pixel-identical (363.8px) between the two tables; weapon name/tag column visibly roomier.

## 6. Final review pass

- [x] 6.1 Run the existing `dotnet test` suite to confirm no regression in
      `LivePlayPhaseTurnRelevanceTests` or other unaffected coverage (473 passed, 13 skipped
      corpus-scan tests unaffected, 0 failed)
- [x] 6.2 Real-device or emulator screenshot pass (per `.claude/implementation-notes.md`) covering
      all five changes together on one page render, in both portrait (375×539) and landscape
      (667×315). Verified via `chrome-devtools`: portrait still shows the unaffected rotate prompt;
      a full-page landscape screenshot against a real imported roster (5 units) confirms every
      change renders coherently together.
- [x] 6.3 Update `.claude/domain-model-11e.md` and `.claude/design-tokens.md` per this project's
      Documentation Maintenance convention (`CLAUDE.md`) to reflect the toolbar tap-target change,
      the phase/turn-driven unit-block collapse (both directions), and the army header's whole-block
      collapse plus the phase tracker's new placement outside it

## 7. Keyword filter fixes (found during verification - see design.md Decision 5)

- [x] 7.1 In `live-play.js`'s `applyKeywordHighlighting()`, also set `unitEl.open = true` alongside
      `section.open = true` when a unit's Keywords section has a flagged match, so the match is
      visible even when that unit's block is collapsed
- [x] 7.2 In `renderArmyKeywordChips()`'s chip click handler, compute an `activating` flag before
      mutating `activeKeywordFilters`, and call a new `scrollToFirstKeywordMatch(key)` helper only
      when `activating` is true
- [x] 7.3 Implement `scrollToFirstKeywordMatch(key)`: find the first `.unit-block` (in page order)
      whose Keywords section contains a chip normalizing to `key`, and call
      `scrollIntoView({block: 'start', behavior: 'smooth'})` on it
- [x] 7.4 Manually verify: with all unit blocks collapsed (via "My Turn"), activating a keyword
      that matches one unit expands only that unit's block and Keywords section, flags the matching
      pill, and scrolls it to the top of the viewport; non-matching units remain collapsed.
      Verified live via `chrome-devtools` with "Epic Hero" (mid-page match) and "Vehicle" (last-unit
      match, confirmed scrolled to `rectTop: ~0`)
- [x] 7.5 Re-run `dotnet test` to confirm no regression (473 passed, 13 skipped, 0 failed)
