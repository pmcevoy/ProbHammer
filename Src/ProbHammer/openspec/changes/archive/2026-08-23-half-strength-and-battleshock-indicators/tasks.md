## 1. Domain model (`ProbHammer.Core.Domain.Roster`)

- [x] 1.1 Add a `HalfStrengthResolution` pure-function resolver (alongside `ToughnessResolution`/
      `KeywordResolution`) computing combined starting strength (`Σ ModelLine.Count` across
      `Components`) and current strength (`Σ ModelLine.RemainingCount`), returning at-or-below-half
      per `floor(starting / 2)`, only meaningful when starting strength >= 2.
- [x] 1.2 Add a player-set half-strength override property to `Unit`/`AttachedUnit` (via their
      `ICombatUnit` implementations), defaulting to false, used only when combined starting
      strength == 1.
- [x] 1.3 Add a player-set Battle-shocked property to `Unit`/`AttachedUnit`, defaulting to false,
      with a clear setter — never mutated by any other domain operation (casualty adjustment,
      half-strength change).
- [x] 1.4 Add a single read that combines 1.1-1.3 into the one at-or-below-half-strength status a
      caller needs, without leaking which of the two sources (computed vs. player-set) produced it.

## 2. Session-backed sync plumbing (`ProbHammer.Web`)

- [x] 2.1 Extend the existing casualty-sync request/response shape (`LivePlayCasualtyService` and
      its endpoint) to also accept a per-unit-index map of half-strength/Battle-shocked toggles,
      reapplying them onto the freshly rebuilt roster the same way casualty adjustments already are
      — one POST, one rebuild, one re-rendered fragment.
- [x] 2.2 Apply the reapplied toggles before `_UnitBlock.cshtml` renders, so the returned fragment
      reflects both the posted casualty state and the posted toggle state in one response.

## 3. Rendering (`_UnitBlock.cshtml`, `site.css`)

- [x] 3.1 Convert `.unit-name` from plain text to a flex row (`display: flex; align-items: center`,
      matching `.lp-section summary`'s existing pattern), name left, a right-aligned glyph group via
      `margin-left: auto` on the group.
- [x] 3.2 Render the `½` glyph per the two starting-strength modes: an interactive `<button>` for
      starting strength == 1 (ordinary text color / amber per its own toggle state), a
      non-interactive `<span>` for starting strength >= 2 (hidden unless at/below half-strength,
      amber when shown) — mirroring the existing `.filter-flag` button/`.filtered-badge` span split.
- [x] 3.3 Add the `⚡` Battle-shock glyph as an inline SVG `<button>`, always rendered, rightmost in
      the group — one `<path>`, `fill: none; stroke: currentColor` when inactive, `fill:
      var(--amber); stroke: none` when active — matching `.filter-flag-icon`'s existing
      inline-SVG-over-Unicode precedent.
- [x] 3.4 Generalize `.insv-tile-caveated`'s amber-tint background rule into a shared class (e.g.
      `.stat-tile-flagged`) usable by both the InSv tile and the new OC tile, without changing the
      InSv tile's own current rendering.
- [x] 3.5 Render the OC tile's flagged state when the owning combat unit is Battle-shocked: shared
      flagged-tile background, `⚡` in place of the numeric value in ordinary `.stat-value` color
      (never amber), label stays `OC`, no caption text underneath — applied to every component's OC
      tile in the block, not only the component nearest the header.

## 4. Client-side toggle controls (`live-play.js`)

- [x] 4.1 Add click handlers for the two new header controls, following the existing
      `initCasualtyControls`/`initCasualtyReset` pattern (`stopPropagation`, immediate DOM update,
      then sync).
- [x] 4.2 Add `localStorage` read/write for the two new per-unit-index toggle maps, separate from
      the existing casualty map's key scheme.
- [x] 4.3 Extend the existing sync call to POST both new maps alongside casualty adjustments, and
      apply the returned fragment via the existing `swapUnitBlock` path.
- [x] 4.4 Confirm the swapped-in fragment's open/closed `<details>` sections are preserved across a
      half-strength/Battle-shock toggle the same way they already are across a casualty adjustment.

## 5. Tests

- [x] 5.1 `HalfStrengthResolutionTests` (`tests/ProbHammer.Tests/Domain/Roster/`): the `floor(n/2)`
      threshold, the 3-model/2-casualty boundary, and combined-AttachedUnit starting strength,
      matching the scenarios in `specs/attached-unit-tracker/spec.md`.
- [x] 5.2 Tests for the player-set half-strength override and Battle-shocked flag's default value,
      independence from each other, and independence from casualty adjustments.
- [x] 5.3 Extend `LivePlayCasualtyEndpointTests` (or add a sibling test file) covering the new sync
      payload: toggles round-trip through a POST, persist across a simulated rebuild, and don't
      disturb existing casualty state.
- [x] 5.4 Extend/parallel `LivePlayInvulnerableSaveRenderingTests` with a rendering test file for
      the header glyphs and the Battle-shocked OC tile, covering: single-model vs. multi-model
      glyph modes, glyph ordering, and every component's OC tile flipping together for an
      AttachedUnit.

## 6. Documentation

- [x] 6.1 Update `.claude/domain-model-11e.md`'s Roster Context section with the new
      `HalfStrengthResolution` pure function and the two new `ICombatUnit` live-state properties,
      per this project's "after every implementation change, update `.claude/`" convention.
- [x] 6.2 Update `.claude/design-tokens.md` with the generalized flagged-stat-tile class and the
      "controls get amber ink when active, stat values never do" rule this change establishes.
- [x] 6.3 Add the "collapse all sections" idea (raised during exploration, deliberately out of
      scope here) to `.claude/vnext-ideas.md`.
