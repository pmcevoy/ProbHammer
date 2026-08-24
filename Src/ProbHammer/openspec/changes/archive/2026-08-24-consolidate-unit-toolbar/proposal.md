## Why

On a small screen, a long unit name (e.g. "Crusader Squad with High Marshal Helbrecht and Crusade
Ancient") wraps in the `.unit-name` header, and the Half-Strength/Battle-shock glyphs that live
right-aligned in that same row get pushed out of view or lost in the wrap. Widening the header
isn't a fix — names will always be able to outgrow it. Separately, the Casualties reset control
currently lives in the Statline section's own summary bar, next to an unrelated Clear-filter
control, which splits the "unit status" controls across two different locations for no reason
tied to what they do.

## What Changes

- **BREAKING** (markup/visual only, no behavior change): Remove the Half-Strength and Battle-shock
  glyphs from `.unit-name`'s own row, and the Casualty-reset button from the Statline section's
  `.statline-flags` bar.
- Add one new toolbar row, rendered between the unit-name header and the Statline section, holding
  all three controls: Half Strength, Battle-shock, Reset Casualties — always present (no more
  hide-when-irrelevant per control), centered, separated by vertical rules.
- Each control communicates two independent, always-visible states via existing tokens only (no
  new colors introduced):
  - **Actionable vs. not actionable** (background): `--bg3` (bottle green) when clickable,
    `--text-dim` when inert — reusing the same background swap idea `.casualty-btn`'s existing
    `:disabled` opacity treatment expresses today, but as a color swap instead of a fade.
  - **Active vs. inactive** (glyph color): normal (white) vs. `--amber` — new for these controls,
    not used elsewhere as a glyph-only signal today.
- The multi-model Half-Strength indicator (today a plain `<span>`, never interactive) becomes a
  permanently-`disabled` `<button>` like the other two, always rendered, with its active/inactive
  glyph color as its only signal (collapses the existing span/button branch into one shape).
- Battle-shock keeps its existing inline-SVG bolt icon (sized in `em`, already zoom-correct);
  confirmed not to switch to a Unicode `⚡` character, which defaults to color-emoji presentation
  on most platforms and would ignore the new white/amber color scheme entirely.

## Capabilities

### Modified Capabilities
- `live-play-view`: "Unit Header Status Glyphs" requirement is replaced by a new toolbar placement
  and always-rendered/dual-state-color model; the Casualty-reset control's placement (currently
  described under the Statline section requirement) moves into the same toolbar.

## Impact

- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — header markup, new toolbar markup.
- `src/ProbHammer.Web/wwwroot/css/site.css` — remove `.unit-status-glyphs`/header-glyph rules and
  the old `.statline-flags`-scoped casualty-reset styling; add `.unit-toolbar` and its button
  states.
- `src/ProbHammer.Web/wwwroot/js/live-play.js` — selectors are class-based and unit-scoped already;
  expected to need no logic changes, only re-verification against the new always-a-button markup.
- `tests/ProbHammer.Tests/Web/LivePlayHalfStrengthAndBattleShockRenderingTests.cs` — several
  assertions hardcode the old `filter-flag status-glyph-*` class strings and the span-vs-button
  distinction for the multi-model case; these need updating to match.
- `.claude/design-tokens.md` — document the new toolbar and its actionable/active color model.
