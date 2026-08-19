## 1. Fixtures

- [x] 1.1 Add a Howling Banshee-based `Datasheet` fixture to `Examples/Datasheets.cs`, using the
      real verified values traced during design (`M8" T3 Sv4+ W1 Ld6+ OC1`, InSv text `"4+* / 5+"`
      resolving to `InvulnerableSave(5, 5, Caveated: true, ...)`, linked ability text "Models in
      this unit have a 4+ invulnerable save against melee attacks."), with a doc comment mirroring
      `CanisRex()`'s own provenance-stating convention.
- [x] 1.2 Add a corresponding `Unit`/roster entry (`Examples/Units.cs` and `View.Roster()`/
      `MyArmyRoster()`) so the new fixture actually appears on `/LivePlay`.
- [x] 1.3 Add a second, explicitly synthetic `Datasheet` fixture exercising the differing-both-known
      invulnerable save case (e.g. `InvulnerableSave(4, 5, Caveated: false, CaveatAbility: null)`),
      with a doc comment stating plainly that it is hand-constructed, not sourced from real BSData,
      since no real corpus text produces this shape today.
- [x] 1.4 Add a corresponding `Unit`/roster entry for the synthetic fixture.

## 2. Rendering

- [x] 2.1 Replace the `.stat-subvalue`/`.stat-subvalue-caveated` block nested inside the Sv tile
      (`_UnitBlock.cshtml:82-107`) with a new element rendered below the `.statline-tiles` row,
      covering all five `InvulnerableSave` shapes from the updated `live-play-view` spec: absent
      (nothing rendered), uniform, melee-only, ranged-only, differing-both-known, and caveated.
- [x] 2.2 Add the side label per shape (`InSv vs ⚔ / ⌖`, `InSv vs ⚔`, `InSv vs ⌖`, or bare `InSv*`
      for caveated).
- [x] 2.3 Render `CaveatAbility.Text` in italics beneath the box for the caveated case, at the unit
      block's normal reading width — not constrained to the box's own column width. (Italic styling
      lands in the CSS pass, task 3.3 - markup and text are in place now.)
- [x] 2.4 Add a code comment at the render site (and a short note in `domain-model-11e.md`) stating
      that this is a scoped, narrow exception to the page's name-only ability rendering, not a step
      toward general full-text rendering.
- [x] 2.5 Confirm the new box and caveat text sit inside the same DOM region the existing
      run-collapse mechanism already hides/shows (`.statline-cell.col-statline.run-collapsed
      .statline-tiles`), so a fully-dead or fully-deselected run collapses them together without new
      JS. (Both rendered as direct children of `.statline-tiles`, no CSS selector change needed.)

## 3. Styling

- [x] 3.1 Add base (non-caveated) InSv box styles reusing `.stat-tile`'s visual language (light
      `--bg` background, `--border` border, black `--text` value, matching border-radius), sized to
      sit below the stat-tile row with a side label rather than a stacked one.
- [x] 3.2 Add a new light-amber-tint custom property (distinct from the existing `--amber` token,
      which stays as-is for its current uses — casualty-reset icon, filter badge) plus the caveated
      box's background/foreground styles. (`--amber-tint: color-mix(in srgb, var(--amber) 20%,
      white)` — starting value, to be checked live in task 4.4.)
- [x] 3.3 Style the caveat ability text line (italic, spacing consistent with the page's existing
      type scale).
- [x] 3.4 Remove the now-unused `.stat-subvalue`/`.stat-subvalue-invsv-split`/
      `.stat-subvalue-caveated` rules once the new box fully replaces them.

## 4. Live verification and refinement

- [x] 4.1 Try the melee glyph (⚔) live in a rendered page; confirm it inherits `currentColor` as
      expected rather than rendering as a full-color emoji. (Confirmed — renders text-coloured,
      thin but legible; kept.)
- [x] 4.2 Try ranged-glyph candidates (⌖, an inline SVG, or another text-presentation glyph) live;
      settle on one. (⌖, ◎, and ⊙ all tried live — each rendered ambiguously in this browser/OS
      font stack, reading as a diamond, an "info" icon, or a plain dot rather than anything
      ranged/target-like. Settled on a small inline SVG crosshair instead, matching this file's
      own filter-flag icon precedent.)
- [x] 4.3 Try box silhouette candidates (plain rounded rect vs. a shield closer to the GW/Wahapedia
      source) live; settle on one. (Kept the plain rounded rect, matching `.stat-tile` — reads as
      part of the same tile family; also settled, per direct user feedback during this session,
      that the box sits in its own CSS Grid column directly under Sv rather than left-aligned at
      column 1, matching the real GW/Wahapedia layout precisely.)
- [x] 4.4 Try the light-amber tint value live, alongside the page's existing `--amber` usages
      (casualty-reset icon, filter badge), for contrast and to confirm it doesn't read as the same
      signal; settle on a value. (`color-mix(in srgb, var(--amber) 20%, white)` confirmed legible
      live — black text on the tint, amber border, distinct from the plain box.)
- [x] 4.5 Check both real caveat sentences now in fixtures (Canis Rex's and the new Howling Banshee
      fixture's, which differ in length and wording) for awkward wrapping at this page's compact
      type scale; adjust spacing if needed. (Both render on one line at this page's default width,
      no wrapping issues. Found and fixed a real bug in the process: `.statline-tiles`' pre-existing
      `max-height: 6rem` — sized for the old, shorter inline layout — was silently clipping the
      caveat text; raised to `12rem`.)

## 5. Tests

- [x] 5.1 Add unit tests covering all five `InvulnerableSave` render shapes directly against
      hand-built `AttachedUnitAggregateView` input, rather than relying only on the example roster
      to happen to exercise each one. **Adjusted from the plan**: the InSv branching lives entirely
      in `_UnitBlock.cshtml`, not in `LivePlayModel.BuildUnitBlock` (which only carries the
      `Statline`/`InvulnerableSave` value through unchanged) — so the `internal BuildUnitBlock`
      approach alone couldn't exercise the actual rendering. New
      `tests/ProbHammer.Tests/Web/LivePlayInvulnerableSaveRenderingTests.cs` instead renders through
      the real `IRazorPartialRenderer`/`_UnitBlock.cshtml` path (the same one `/LivePlay` and the
      casualty endpoint use), `WebApplicationFactory`-hosted, one test per shape (absent, uniform,
      melee-only, ranged-only, differing-both-known, caveated) asserting on the rendered HTML.
- [x] 5.2 Add/update any existing test asserting on the old `.stat-subvalue` rendering shape, if
      present. (None found — no existing test asserted on that markup directly.)
- [x] 5.3 Run the full test suite (`dotnet test`) and confirm a clean pass. (180 tests, 178 run + 2
      explicit-only corpus scans skipped as expected, all passing. Two pre-existing tests needed
      updating for the roster growing from 7 to 9 units:
      `ViewTests.Roster_UnitsMatchesMyArmyRoster`'s hardcoded count, and
      `LivePlayModelTests.OnGet_OrdersAttachedUnitsBeforePlainUnits...`'s expected unit-name order —
      both straightforward updates confirming the existing sort rule placed Howling Banshees/
      Twin-Ward Sentinel correctly, not a real behavior gap.)

## 6. Manual verification

- [x] 6.1 Visually verify via `firefox-devtools-mcp` against the real example army: an existing
      uniform-value unit (e.g. High Marshal Helbrecht, InSv 4), the new Howling Banshee caveated
      fixture, the new synthetic differing-both-known fixture, and Canis Rex (unchanged, confirming
      no regression). (All four confirmed rendering correctly — see the 4.x notes above for the
      layout/glyph refinements this check drove.)
- [x] 6.2 Confirm run-collapse (fully-dead and fully-deselected) hides and restores the InSv box and
      caveat text correctly, using a unit that has one. (Marked High Marshal Helbrecht as a
      casualty — run collapsed to header-only, InSv box included; reverted via `+` — box and label
      reappeared correctly, no JS changes needed.)

## 7. Documentation

- [x] 7.1 Update `.claude/design-tokens.md` for the new box styling and the new light-amber-tint
      token.
- [x] 7.2 Update `.claude/domain-model-11e.md` if the fixture additions or the ability-text scoped
      exception need a mention there beyond the render-site comment. (Evaluated — skipped: that doc
      covers the domain layer only and doesn't catalog individual `Examples/` fixtures, not even
      `CanisRex()`; this change makes no `Domain/Catalogue`/`Domain/Roster`/`Bsdata` change at all,
      so there's nothing there for it to update.)
- [x] 7.3 Update `PROGRESS.md` with an implementation summary once complete.

## 8. Post-review refinement (second live feedback round)

Prompted by user feedback after seeing the first implementation rendered, covering four related
points at once:

- [x] 8.1 Move the "InSv" label inside the tile (matching M/T/Sv/W/Ld/Oc's own label-above-value
      layout) instead of beside a separate value-only box.
- [x] 8.2 Fix the column-width bleed this same change caused into the W/Ld/Oc tiles: confine the
      InSv tile to a single grid column (`grid-column: 3`, matching Sv) instead of spanning columns
      3 through the row's end — a spanning row let a wide label force every column it crossed wider.
- [x] 8.3 Scale the value text smaller (`.stat-value-compact`, `0.72rem`) for the differing-both-known
      case specifically, so its two-value line doesn't force the Sv column much wider than its
      neighbours.
- [x] 8.4 Move the attack-type icons off the label (which is now always plain "InSv") and onto the
      value line itself, directly beside the number each icon qualifies.
- [x] 8.5 Update the `LivePlayInvulnerableSaveRenderingTests` assertions for the new markup
      (`insv-tile`/`insv-tile-caveated` class names, icon-beside-value rather than icon-in-label
      text) and re-run the full suite — 180 tests, all passing.
- [x] 8.6 Re-verify all five shapes live via `firefox-devtools-mcp` (uniform, caveated ×2, and the
      differing-both-known case) — labels now read "INSV"/"INSV*" (auto-uppercased by the reused
      `.stat-label` class, matching M/T/SV/etc. exactly), other tiles back to their original compact
      width, `⚔4+ / [crosshair]5+` reads legibly at the smaller compact size.
- [x] 8.7 Update `specs/live-play-view/spec.md` and `design.md` to describe the tile-with-inline-icon
      shape rather than the superseded box-plus-side-label one.

## 9. Post-review fine-tuning (third live feedback round)

Prompted by closer visual inspection once the tile-based shape landed; the user explicitly asked to
hold spec/design updates until this round settled, then approved folding them in.

- [x] 9.1 Tighten `.stat-tile`'s `padding`/`min-width` ("very spacious") — applies to all six
      M/T/Sv/W/Ld/Oc tiles as well as InSv, not an InSv-specific change.
- [x] 9.2 Confirm, via a live side-by-side mockup (not just re-assert from memory), that the SVG
      crosshair genuinely reads better than U+2316 (⌖) in this browser/OS font stack — the user asked
      "was there a reason" for the SVG, so this needed re-proving, not just restating.
- [x] 9.3 Drop the `/` separator from the differing-both-known case and add a space between each
      icon and its value (`⚔4+ / [icon]5+` → `⚔ 4+ [icon] 5+`) — first iteration on the "not great"
      feedback, before the bigger layout change in 9.4/9.5.
- [x] 9.4 Mock up two alternative differing-both-known layouts live (temporary DOM injection, no
      source changes) before touching real markup: icon-over-value pairs joined by `/`, vs. two
      stacked rows (ranged above melee, no separator) — presented side by side with the shipped
      version for direct comparison.
- [x] 9.5 Implement the chosen layout (stacked rows, ranged above melee) for real in
      `_UnitBlock.cshtml`/`site.css`; `.stat-value-compact` changed from a single smaller-font line
      to a `flex-direction: column` pair of rows. No test changes needed — the existing assertions
      already matched the new markup's actual text content.
- [x] 9.6 Confirm live, via a second mocked-up comparison, that the melee-only/ranged-only icon
      already distinguishes those shapes from a bare uniform value — raised by the user thinking
      ahead to a possible future where a still-deferred ability-text-interpretation pass flips a
      caveated save (e.g. Canis Rex) to a genuine ranged-only one. No code change needed; this was
      already true of the existing branch logic, just not yet visually confirmed.
- [x] 9.7 Run the full test suite after each markup/CSS change in this round — 180 tests, all
      passing throughout.
- [x] 9.8 Update `specs/live-play-view/spec.md` (the differing-both-known scenario and narrative
      bullet, now describing two stacked rows rather than one joined line) and `design.md` (tile
      compactness, the re-confirmed ranged-glyph reasoning, and the two-round mockup process behind
      the final stacked-row layout) — deferred until this round settled, per explicit user request,
      then done once approved.
