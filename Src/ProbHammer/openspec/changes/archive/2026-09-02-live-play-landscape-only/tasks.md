## 1. Legacy CSS Removal

- [x] 1.1 Delete the confirmed dead block in `site.css` (lines 64-500 as measured pre-change —
      `.index-page`, `.catalogue-bar`, `.unit-card` and all its descendants/modifiers including
      `.unit-card.selected-defender`, `.combat-panel`, `.mod-section-*`, `.sim-stats`,
      `.pipeline-table`, and everything between). Also removed a second orphaned leftover found
      during the scan below: a `@media (max-width: 700px)` block outside the main range that only
      targeted now-deleted `.army-columns`/`.army-inputs`/`.catalogue-bar`.
- [x] 1.2 Diff the bare `.weapon-table` / `.weapon-table th` / `.weapon-table td` /
      `.weapon-table td.weapon-name` rules' declared properties against
      `.live-play-page .weapon-table`'s own rule set, property by property. Also diffed the bare
      `.unit-name` rule against `.unit-block .unit-name` (not flagged in the proposal, but caught by
      the same block-removal): fully covered (its `flex: 1` is inert — `.unit-block` is not a flex
      container), safe to remove outright.
- [x] 1.3 Remove the bare `.weapon-table`/`th`/`td`/`.weapon-name` rules only for properties fully
      covered by the prefixed rule; fold forward into `.live-play-page .weapon-table` any property
      that isn't, then remove the bare selector. Result: every property was fully covered by the
      prefixed rule (or the prefixed rule's higher specificity already wins on the differing ones),
      so no fold-forward was needed here — all four bare rules removed outright.

      One rule in the dead block did need folding, not flagged by the proposal: `.mod-step-btn`
      (from the legacy "Modifier controls" section) is actually live — every casualty inc/dec
      button in `_UnitBlock.cshtml` carries `class="casualty-btn mod-step-btn casualty-dec/inc"`,
      relying on `.mod-step-btn` for its sizing/shape/background/hover and `.casualty-btn` only for
      the color override. Folded `.mod-step-btn`'s full declaration (plus its `:hover`) into
      `.casualty-btn` to preserve pixel-identical behavior, then removed the now-redundant
      `mod-step-btn` class token from all four button instances in `_UnitBlock.cshtml` (and the one
      prose comment naming it) since its CSS purpose is gone.
- [x] 1.4 Remove the now-orphaned `.weapon-row.selected-attacker` and
      `.weapon-row.weapon-type-locked` declarations. Confirmed `.weapon-row` itself (bare, no
      modifier) is never used in current markup either (only `.weapon-row-alt`/
      `.weapon-contribution-row`), so `.weapon-row:hover td` and `.weapon-table td.abilities-cell`
      were removed too as part of the same confirmed-dead group.
- [x] 1.5 Re-run the orphan-class scan (class selectors in `site.css` vs. every `.cshtml`, `.cs`
      in `ProbHammer.Web` and `ProbHammer.Core`, and `.js`) to confirm no new orphans were
      introduced and none of the removed classes were actually reachable. Found and fixed the two
      real issues above (`.mod-step-btn` folded rather than deleted; a second orphaned `@media`
      block); every other flagged candidate was a false positive from crude token-extraction
      (dynamically-built class strings in C#/JS, or comment-only mentions) verified by direct grep.

## 2. Orientation Gate

- [x] 2.1 Add the rotate-prompt overlay markup to `LivePlay.cshtml` (visually consistent with the
      existing GW-datasheet theme — reuse existing tokens, no new colors). Icon is a hand-authored
      inline SVG (Material's "screen rotation" glyph shape), not a Unicode/emoji character —
      matching this codebase's established convention (`.claude/design-tokens.md`'s
      `.unit-toolbar-svg-icon` note) that emoji glyphs ignore `fill`/`currentColor` on most
      platforms.
- [x] 2.2 Add `@media (orientation: portrait) and (max-width: 600px)` rule(s) in `site.css`:
      hide the roster content, show the overlay.
- [x] 2.3 Confirm the roster still renders unconditionally server-side underneath the gate (the
      gate is CSS-only — no server-side user-agent/viewport branching). Confirmed: `.rotate-prompt`
      and `.live-play-content` both render unconditionally in `LivePlay.cshtml`; only the `@media`
      rule in `site.css` decides which is visible.
- [x] 2.4 Verify in the emulator: narrow+portrait shows the prompt, narrow+landscape shows the
      roster, wide (tablet-class, e.g. 768px)+portrait shows the roster, and rotating narrow
      portrait→landscape live-swaps with no reload and no lost casualty/status state. Verified
      live against a real imported roster (Templars, `data/gw-app-export-templars.txt`) via
      chrome-devtools-mcp: 375x539 portrait shows the prompt (`.rotate-prompt` visible, icon
      renders correctly); 667x315 landscape shows the roster; 768x1024 portrait shows the roster
      (`.rotate-prompt` `display:none`, `.live-play-content` `display:block`); toggling the
      emulated viewport between 375x539 portrait and 667x315 landscape swapped the prompt/roster
      live with no navigation — confirmed by a prior casualty edit and a collapsed unit-block's
      `open:false` state both surviving the viewport change (only possible with no reload).

## 3. Army Rules Section Collapse

- [x] 3.1 Convert the Rules section's markup in `_ArmyHeader.cshtml` from a plain `<div>` title
      bar to a `<details class="lp-section">`/`<summary>` pair, matching the All Keywords
      section's existing shape exactly. Found already done: the current code already has this
      exact `<details class="lp-section" data-section="army-rules" open>`/`<summary>` structure
      (design.md's premise that it's "deliberately a plain `<div>`" is stale — some earlier,
      unrelated change already converted it). Only the `open` attribute needed removing.
- [x] 3.2 Confirm it renders closed by default (no `open` attribute) and that
      `RulePopoverRenderer`-built triggers inside it still work once nested under `<details>`.
      Confirmed: `RulePopoverRenderer`'s triggers are plain `popovertarget` buttons, unaffected by
      an ancestor `<details>`'s open/closed state; `live-play.js` has no reference to
      `data-section="army-rules"` at all (it isn't part of the fragment-swap carry-forward logic),
      so removing `open` is a fully self-contained change.
- [x] 3.3 Verify existing Army Header scenarios (Detachment rule triggers, army-wide column
      omitted when empty, etc.) still pass with the new markup shape. All 7 existing
      `LivePlayArmyHeaderRenderingTests` (Detachment 0/1/many-rule triggers, ArmyRule column
      shown/omitted, no DP/points cost rendered) pass unchanged. Also confirmed live in the
      emulator: expanding the collapsed-by-default Rules section shows the Templars roster's real
      "Templar Vows" (Army) and "Righteous Fervour" (Companions of Vehemence Detachment) rules, and
      tapping the "Templar Vows" trigger opens its popover correctly while nested under the now-
      collapsible `<details>`.

## 4. Unit Block Full Collapse

- [x] 4.1 Convert `.unit-block`'s root element in `_UnitBlock.cshtml` from `<section>` to
      `<details>`, and `.unit-name` from `<h2>` to `<summary class="unit-name">` (or an `<h2>`
      nested inside the `<summary>`, whichever keeps heading semantics intact). Went with the
      direct swap (plain `<summary class="unit-name">`, no nested `<h2>`) matching design.md's own
      stated decision text and `.lp-section summary`'s existing precedent (which also has no
      nested heading tag).
- [x] 4.2 Grep `site.css` and `live-play.js` for any element-qualified `.unit-block` selector
      (e.g. `section.unit-block`) that assumes the old element type; update any found. None found —
      every existing selector/query is class-only (`.unit-block`, `.unit-block[data-unit-index=...]`),
      so no updates were needed.
- [x] 4.3 Confirm the existing per-section `<details class="lp-section">` elements still nest and
      behave correctly inside the new outer `<details>` (collapsing the outer one hides them
      without altering their own `open` state; re-expanding restores it). Nested `<details>` is
      valid HTML with independent per-level `open` state (browser-native, no app logic needed);
      confirmed the outer/inner `<details>` tags balance correctly (5 opens, 5 closes) after the
      markup conversion.
- [x] 4.4 Default every unit block to `open` (expanded) on initial render. Server always renders
      the outer `<details class="unit-block" ... open>` with the `open` attribute present.
- [x] 4.5 In `live-play.js`, extend the existing fragment-swap carry-forward logic
      (`swapUnitBlock`) to also read and reapply the old DOM's own top-level
      `<details class="unit-block">.open` state unconditionally (no server-forced case exists for
      this level, unlike the inner sections' `forcedSections`).
- [x] 4.6 Verify: collapsing a unit block hides its toolbar and all sections leaving only the name
      bar; collapsing one unit block doesn't affect others; an unrelated casualty/phase-turn sync
      preserves a previously-collapsed block's collapsed state. Verified live: collapsing
      "Crusader Squad with High Marshal Helbrecht and Crusade Ancient" left only its name bar
      visible (▶) while the next block stayed fully expanded (▼, toolbar + sections); marking a
      casualty on a different, expanded unit (a fragment-swap sync affecting only that unit) left
      the collapsed block at `open:false`; a phase/turn tracker click (which re-renders every unit
      block) also left it at `open:false` — carry-forward confirmed for both sync paths.

## 5. Statline Grid: Merge Model And Unit Abilities Into One Column

Superseded mid-implementation, at the user's own explicit request after the real-device round-trip
(task 7.4): the original per-unit conditional 2-vs-3-column dead-column fix (below, landed first)
is replaced by always merging Model and Unit Abilities into one stacked column — the user recalled
that the two-column split existed to distinguish model-scoped from unit-scoped abilities, decided
that distinction doesn't need its own column, and floated a future per-ability scope marker
(mirroring the Enhancement "✦" prefix) as the way to surface it again if wanted. This also fully
subsumes the original dead-column problem: with only one abilities column, "a unit has no Model
Abilities anywhere" is no longer a case that needs detecting at all.

- [x] 5.1 (Superseded) Originally: compute per-unit-block whether any row-group has a Model
      Abilities entry, to decide a 2- vs 3-column grid. Replaced by unconditionally merging
      `ModelAbilities`/`UnitAbilities` at render time in `_UnitBlock.cshtml` (`.Concat()`, Model
      first then Unit, matching the old left-to-right column order) into one `.col-abilities` cell
      per row-group/span — no per-unit computation needed. Removed the now-dead
      `UnitBlockViewModel.HasAnyModelAbilities` property this task had added.
- [x] 5.2 (Superseded) Originally: a computed 2-vs-3-column `grid-template-columns` plus a computed
      `unitAbilitiesColumn` (2 or 3) per cell. Replaced by a single fixed 2-column
      `.statline-grid` template (`minmax(0,2fr) minmax(0,1fr)`) — every abilities cell (row-bound,
      `spans-component`, `spans-whole-unit` alike) is now unconditionally `grid-column: 2`, no
      per-unit branching in `_UnitBlock.cshtml` or `site.css` at all. `.statline-cell.col-model-
      abilities`/`.col-unit-abilities`'s three shared CSS rules (base box, `align-self: start`,
      `run-collapsed`) merged into one `.statline-cell.col-abilities` selector each. The domain/
      view-model split (`Ability.Scope`, `StatlineBlockViewModel.ModelAbilities`/`UnitAbilities`,
      and the `ComponentAbilitySpanViewModel`/`WholeUnitAbilitySpanViewModel` equivalents)
      deliberately stays untouched — only the Razor rendering merges the two lists, so a future
      scope marker has the data it needs with no domain rework.
- [x] 5.3 Verify: a row-group/span with abilities of either or both scopes renders them as one
      stacked list in a single column immediately adjacent to the statline card, in every case (not
      only the "no Model Abilities" case the original fix targeted). Updated the one existing test
      that asserted on the old `col-unit-abilities` class name
      (`LivePlayAbilityRenderingTests.ARowBoundAbility_AbsorbsIntoASingleRowComponentWideSpan_InsteadOfColliding`)
      to assert on `col-abilities` instead; removed the 3 `HasAnyModelAbilities_*` unit tests added
      by the superseded 5.1 (the property they covered no longer exists). Full suite: 486/486
      passing (13 explicit corpus-scan skips, unrelated). Verified live in the emulator against the
      real Templars roster: `.statline-grid` computes to a fixed 2-column
      `grid-template-columns` for every unit block, zero horizontal overflow at 667×315 landscape.
      As before, every unit in this specific roster has zero Model-scoped abilities, so the mixed
      Model+Unit-in-one-box case wasn't re-exercised live — covered by
      `BuildUnitBlock_RowBindsModelLineAbilities_AndSpansComponentWideAbilitiesAcrossTheirRuns` and
      the updated `ARowBoundAbility_...` test instead.

## 6. Name-Bar and Section-Title Font Sizes

- [x] 6.1 Reduce `.lp-section summary`'s font-size from its current `0.82rem`. Set to `0.72rem`.
      Also reduced `.rule-popover-title` the same way — not named in design.md/tasks, but
      design-tokens.md documents it as a deliberate byte-for-byte duplicate of `.lp-section
      summary`'s own section-title-bar styling (background/color/weight/size/case/letter-spacing),
      so leaving its size behind would break that established visual parity.
- [x] 6.2 Reduce `.army-header-name` and `.unit-block .unit-name` font-size from `1rem` to a value
      that stays a step above the new (smaller) section-title size; tune all three values together
      visually in the emulator against the landscape height budget. Set both to `0.88rem` as a
      first pass (a clear step above `0.72rem`); final tuning happens in the emulator pass (task
      7.2) against the actual measured landscape height budget.

## 7. Verification

- [x] 7.1 Re-run the full-page overflow scan (`scrollWidth > clientWidth` walk) in the emulator at
      375x539 portrait — expect it now only fires inside the (expected, gated) portrait state, not
      as a rendering bug. Zero overflowing elements found (`.live-play-content` is `display:none`
      under the gate, so its internal width never contributes); `document.body.scrollWidth` equals
      `window.innerWidth` exactly (375px).
- [x] 7.2 Measure `.army-header`'s height at 667x315 landscape; confirm it fits within 315px with
      at least a sliver of the first unit block visible without scrolling. Measured against the
      live Templars roster: `.army-header` height ≈222px (down from the pre-change 321px, mostly
      from the Rules section now defaulting closed), first unit block's own top starts at ≈258px,
      leaving a ≈57px visible sliver within the 315px viewport — comfortably passes with no further
      font-size tuning needed.
- [x] 7.3 Confirm `dotnet build`/existing test suite still passes after the markup/CSS changes.
      `dotnet build` succeeds with 0 warnings/0 errors; `dotnet test` passes 489/489 (13 explicit
      corpus-scan tests skipped by design, unrelated to this change) — includes the 3 new
      `HasAnyModelAbilities` tests added for task 5.3.
- [x] 7.4 Final round-trip: real-iPhone screenshots in both portrait (expect rotate prompt) and
      landscape (expect a working, uncramped roster), matching this session's established
      emulator-vs-reality verification discipline. First landscape screenshot found a real bug the
      emulator pass entirely missed — sibling `.unit-name` bars sharing one `font-size` rule
      rendered at visibly different sizes (a wrapped, 2-line name noticeably larger than
      single-line ones). Root cause: WebKit/mobile-Safari's automatic text-size-adjust ("font
      boosting") heuristic, invisible in `chrome-devtools-mcp`'s emulation since it's a
      real-WebKit-only behavior — see `.claude/implementation-notes.md`'s new note. Fixed via
      `html { -webkit-text-size-adjust: 100%; text-size-adjust: 100%; }` in `site.css`. User
      confirmed live on the device: portrait rotate prompt, no-reload rotation, Rules popover,
      unit-block collapse, casualty buttons, the font-boost fix, and the merged abilities column
      (task 5, revised mid-implementation per the user's own request) all working correctly.
