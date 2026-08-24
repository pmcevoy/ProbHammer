## 1. Markup (`_UnitBlock.cshtml`)

- [x] 1.1 Remove `.unit-status-glyphs` and its two glyphs from `.unit-name`, leaving only the name text.
- [x] 1.2 Remove the casualty-reset button from `.statline-flags` (leave the Clear-filter button in place).
- [x] 1.3 Add a new `.unit-toolbar` row between `.unit-name` and the `@if (unit.Statlines.Any())` block, always rendered, containing three always-present `<button>`s (Half Strength, Battle-shock, Reset Casualties) in that order, each with an icon + short text label.
- [x] 1.4 Half Strength button: single-model case renders `data-unit-index`, never `disabled`; multi-model case renders the same button shape permanently `disabled` (no more `<span>` branch). Both drive the glyph's active/inactive color off `IsAtOrBelowHalfStrength`.
- [x] 1.5 Battle-shock button: always enabled; reuse the existing `BoltSvgIcon` helper, glyph color driven off `IsBattleShocked`.
- [x] 1.6 Reset Casualties button: `disabled` when `!unit.HasCasualties`; glyph amber exactly when not disabled.

## 2. Styling (`site.css`)

- [x] 2.1 Remove `.unit-status-glyphs`, `.status-glyph-halfstrength.is-active`, the old `.battleshock-icon` sizing rule, `.status-glyph-battleshock.is-active .battleshock-icon path`, `.casualty-reset-btn`, and `.casualty-reset-btn-emoji` (superseded by the rules below). Leave `.oc-battleshock-icon` untouched (unrelated context).
- [x] 2.2 Add `.unit-toolbar` (centered flex row, divider between items) and `.unit-toolbar-btn` base styles reusing `.mod-step-btn`'s sizing/shape conventions but this page's own tokens (`--bg3` actionable background, `--text-dim` inert background, white/`--amber` glyph color) - no ArmyView hex values.
- [x] 2.3 Style the Battle-shock icon's fill/stroke swap against the new active/inactive glyph-color rule (drop the old outline-vs-solid duality now that color alone carries the signal).
- [x] 2.4 Verify `.unit-block .unit-name` no longer needs its `display: flex`/right-alignment rules now that it holds only the name text; simplify if so.

## 3. Behavior verification (`live-play.js`)

- [x] 3.1 Confirm `initStatusGlyphControls`/`initCasualtyReset`'s existing class-based, unit-scoped selectors keep working unchanged against the new markup location (no logic changes expected - verify, don't assume).
- [x] 3.2 Confirm the multi-model Half Strength button, now real but permanently `disabled`, has no click handler attached (or that one attached defensively is a no-op) - the browser's native `disabled` already blocks clicks, but `initStatusGlyphControls` queries `button.status-glyph-halfstrength` unconditionally today.

## 4. Tests

- [x] 4.1 Update `LivePlayHalfStrengthAndBattleShockRenderingTests.cs`: replace the old `filter-flag status-glyph-*` class-string assertions with the new toolbar markup; flip the multi-model "never a `<button>`" assertions to "always a `<button>`, `disabled` in this case".
- [x] 4.2 Add/adjust assertions covering the new actionable/inert background and active/inactive glyph-color states (per the "Unit Status Toolbar" requirement's scenarios) for all three controls.
- [x] 4.3 Spot-check `LivePlayCasualtyEndpointTests.cs`'s existing substring assertions (`status-glyph-battleshock is-active`) still hold against the swapped-in fragment markup.
- [x] 4.4 Run the full test suite (`dotnet test`).

## 5. Manual verification

- [x] 5.1 Run the app locally, open `/LivePlay` with a real import, and confirm: a long unit name that wraps no longer obscures any control; all three controls always render; actionable/inert and active/inactive read clearly at a glance (including the "inert but active" multi-model half-strength case flagged as a risk in design.md). Verified against the real Templars export (`data/gw-app-export-templars.txt`), whose "Crusader Squad with High Marshal Helbrecht and Crusade Ancient" is the exact long-name case that motivated this change - the toolbar stayed fully visible below the two-line-wrapped header.
- [x] 5.2 Click-test each control (toggle half-strength on a single-model unit, toggle Battle-shock, mark a casualty then Reset) and confirm behavior is unchanged from before this change. Confirmed: single-model Half Strength (Impulsor) and Battle-shock both toggle actionable-and-active correctly; marking a casualty flips Reset Casualties from inert/grey to actionable/amber correctly. The Reset button's own click-through (past the native `confirm()` dialog) could not be exercised in the headless browser session, since headless Firefox auto-dismisses `window.confirm()` - that code path is unchanged by this proposal (design.md's Non-Goals) and covered by existing JS behavior, not a new risk.

## 6. Documentation

- [x] 6.1 Update `.claude/design-tokens.md`'s header-glyph/casualty-reset sections to describe the new toolbar and its actionable/active color model.
- [x] 6.2 Update `.claude/domain-model-11e.md` if it references the old header-glyph placement (check `/LivePlay` wiring notes). Confirmed no references exist there - no change needed.

## 7. Follow-up restyle (post hands-on review)

Direct user feedback after seeing the first build in the browser - see design.md's "Revision (post hands-on review)".

- [x] 7.1 Split each control into a small square icon-only button (`.unit-toolbar-icon-btn`, sized like `.mod-step-btn`/`.casualty-btn`) as the sole click target, plus a separate static `.unit-toolbar-label` caption styled like `.stat-label` - no longer icon+label combined inside one button.
- [x] 7.2 Change the inert-state treatment from a `--bg3`-to-`--text-dim` background swap to `.casualty-btn:disabled`'s own plain `opacity: 0.4` fade, matching the inc/dec buttons' disabled look exactly.
- [x] 7.3 Re-verify in the browser (Templars export) and re-run the full test suite - no test changes were needed (all assertions target markup, not computed style).
- [x] 7.4 Update `.claude/design-tokens.md` and this change's `design.md` to reflect the revised styling.

## 8. Follow-up contrast/icon fixes (post hands-on review, round 2)

Direct user feedback after seeing round 1's restyle live - see design.md's "Revision 2 (post hands-on review, real device/browser)".

- [x] 8.1 Add `--amber-bright` (`color-mix(in srgb, var(--amber) 55%, white)`) to `:root` and point `.unit-toolbar-icon-btn.is-active`'s icon color at it instead of bare `--amber` - fixes `--amber`'s ~2.8:1 contrast against a solid `--bg3` fill, confirmed via direct user testing as not reading as "amber" at all.
- [x] 8.2 Replace the Reset Casualties button's U+1F480 skull emoji with a hand-authored inline SVG (`SkullSvgIcon`, `fill-rule="evenodd"` eye/nose holes), sharing `.unit-toolbar-svg-icon` with the Battle-shock bolt - the emoji ignored `color` entirely, which is why its icon never actually turned amber when active.
- [x] 8.3 Re-verify in the browser (computed-style check confirming the actual applied color, plus visual screenshots) and re-run the full test suite - no test changes needed.
- [x] 8.4 Update `.claude/design-tokens.md` and this change's `design.md` to record both fixes and why.
