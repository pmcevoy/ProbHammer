## Context

See `proposal.md` - Why. Current markup/CSS: the two header glyphs live in `.unit-name` (`_UnitBlock.cshtml`, `.unit-status-glyphs`/`.status-glyph-halfstrength`/`.status-glyph-battleshock` in site.css); the casualty-reset button lives in the Statline section's `.statline-flags`, alongside the unrelated Clear-filter control. All three read state already computed onto the `UnitBlockRenderModel`/aggregate view (`IsSingleModelUnit`, `IsAtOrBelowHalfStrength`, `IsBattleShocked`, `HasCasualties`) - no C# domain changes are needed for this change, it is markup/CSS/JS-only.

## Goals / Non-Goals

**Goals:**
- Move all three controls into one always-rendered toolbar row, immune to header text wrapping.
- Reuse this page's existing color vocabulary only (`--bg3`, `--text-dim`, `--amber`, white) - no new colors.
- Collapse the existing button/span branch for the multi-model half-strength case into "always a button, sometimes `disabled`".

**Non-Goals:**
- No change to the Reset Casualties click behavior itself (confirm dialog, reset-to-initial semantics) - only its placement and always-rendered/inert-vs-actionable presentation.
- No change to persistence (`localStorage` keys/shape) for half-strength/Battle-shock/casualties.
- No attempt to also relocate the Clear-filter control that currently sits in `.statline-flags` - it is unrelated to unit *status* and stays where it is.

## Decisions

**Two independent visual dimensions, not one.** Established during exploration: "actionable vs. inert" (can this control currently be clicked) and "active vs. inactive" (is the fact it represents currently true) are orthogonal, and get separate visual channels:
- Actionable/inert → background color: `--bg3` (bottle green, matching `.casualty-btn`/`.mod-step-btn`'s existing enabled look) vs. `--text-dim`.
- Active/inactive → glyph color: white (matching `.casualty-btn`'s existing icon-color override) vs. `--amber`.

Rejected alternative: reusing `.casualty-btn:disabled`'s `opacity: 0.4` fade for the inert state, and/or `.mod-step-btn:hover`'s hardcoded `#2a3a6a` for an "active" fill. The former collapses both dimensions into one (an inert-and-active control would look identical to inert-and-inactive, exactly the problem the multi-model half-strength case has today - it disappears rather than staying legible at rest). The latter is a stray hex value outside this page's eight-token palette (an ArmyView/dark-theme leftover, not a LivePlay design-token) and was never actually an "active" state to begin with - it is just `:hover`.

**Reset Casualties has no independent "active" fact - its actionable state doubles as the signal.** Unlike Half Strength/Battle-shock, there is no separate boolean it represents beyond "there is at least one casualty to reset." So its glyph is amber exactly when it's actionable, white otherwise - it does not need the general active/inert × actionable/inert 2×2, just actionable-implies-amber.

**Multi-model Half Strength collapses to `disabled`, not a conditionally-omitted `<span>`.** Today it is a passive, non-interactive `<span>` that simply isn't rendered when above half-strength. In the toolbar every control always renders, so the cleanest shape is a real `<button disabled>` for this case too - consistent DOM shape across all three controls, with the browser's native `disabled` semantics blocking clicks, and CSS driving the inert background off `:disabled` the same way `.casualty-btn:disabled` already does. This is a genuine, intentional markup change (see proposal.md's Impact) - `LivePlayHalfStrengthAndBattleShockRenderingTests.cs`'s assertions that this case is "never a `<button>`" need to flip.

**Battle-shock keeps its inline SVG bolt; no switch to a Unicode `⚡` glyph.** Investigated during exploration: `⚡` (U+26A1) defaults to emoji presentation on most platforms, which ignores `currentColor`/the amber-vs-white scheme entirely, and forcing text presentation (`⚡︎` + U+FE0E) has the same cross-platform-inconsistent-glyph problem this codebase already hit once with the invulnerable-save Ranged icon (`⌖`/`◎`/`⊙` all rendered ambiguously, replaced with inline SVG). The existing SVG is already sized in `em`, so it already scales correctly under browser zoom - there was no actual zoom-correctness gap to fix. The SVG also gets simpler under this design (only needs a fill/stroke color swap now, not the old outline-vs-solid duality), so there's no new complexity cost to keeping it.

**Toolbar placement and layout.** One `<div>` row between `.unit-name` and the `@if (unit.Statlines.Any())` block, `display: flex; justify-content: center`, each of the three controls as its own flex item (`.unit-toolbar-item`), separated by a vertical rule (`border-left` on all but the first item) rather than a gap alone - makes the three-way split legible at a glance, matching the "table border to separate the entries" direction from exploration.

### Revision (post hands-on review)

Two decisions above were revised after building the first version and looking at it in the browser and at the table - both are the user's own direct feedback, not a change in requirements:

- **Icon-only button, separate static label - not icon+label combined inside one button.** The first build made the entire pill (icon + text) the click target. Revised so `.unit-toolbar-icon-btn` is a small square containing only the icon - the *only* clickable area, sized exactly like `.mod-step-btn`/`.casualty-btn`'s existing inc/dec controls - with `.unit-toolbar-label` as a plain sibling caption outside the button, styled identically to `.stat-label` (`--text-dim`, `0.65rem`, uppercase) and never itself a status indicator. This is also what makes the earlier "vertical rule between items" idea land the way it was actually described: the border sits between whole icon+label groups (`.unit-toolbar-item`), not between bare buttons.
- **Inert state is an opacity fade (`.casualty-btn:disabled`'s own `opacity: 0.4`), not a `--bg3`-to-`--text-dim` background swap.** The original decision (see the "Two independent visual dimensions" entry above and the Risk below) deliberately avoided opacity specifically to keep an inert-but-active icon's amber fully legible. After seeing both side by side, the user asked for the inc/dec buttons' own exact disabled look instead, so every disabled control on the page reads consistently - accepting the "muddy amber" trade-off that decision was originally trying to avoid (see Risks below).

### Revision 2 (post hands-on review, real device/browser)

After the first restyle above was actually seen live, two more concrete defects surfaced - not aesthetic preference, but things that plain weren't working:

- **`--amber` on a solid `--bg3` fill doesn't pop - confirmed a real, measurable contrast problem, not a subjective one.** `--amber` (`#a86a1d`) against `--bg3` (`#24413f`) computes to roughly 2.8:1 contrast - `--amber` has only ever been used before as ink on this page's light surfaces (`--bg`/`--bg2`), where it reads fine; a small icon painted directly on solid `--bg3` is a genuinely new context that token was never validated against. Fix: added `--amber-bright` (`color-mix(in srgb, var(--amber) 55%, white)`, ~4.9:1 against `--bg3`) and pointed `.unit-toolbar-icon-btn.is-active`'s icon color at it instead of bare `--amber`. This is the same "derive a named, purpose-scoped variant via `color-mix` rather than reach for opacity or a new hue" pattern `--amber-tint`/`--bg3-tint` already established - just the first one scoped to a *foreground* (icon-on-dark) context instead of a background.
- **The Reset Casualties skull was a Unicode emoji (U+1F480) - which is why its icon never actually turned amber.** Most platforms render an emoji as a fixed, full-color glyph that ignores `color`/`currentColor` entirely, so no CSS color rule could ever reach it - the exact class of problem `BoltSvgIcon` was already built to avoid for Battle-shock, just not (initially) recognized as applying to the skull too. Fix: replaced it with a hand-authored inline SVG (`SkullSvgIcon`, `fill-rule="evenodd"` carving the eye sockets/nasal cavity as true holes out of one silhouette), sharing `.unit-toolbar-svg-icon` with the bolt so it responds to white/`--amber-bright` identically.

Both fixes are additive on top of Revision 1 above - no further markup restructuring, no test changes (all existing assertions target markup/classes, not computed color or which Unicode/SVG shape renders).

## Risks / Trade-offs

- **A permanently-dim amber glyph (inert + active) may read as muddy.** Flagged during exploration and deferred to hands-on judgment rather than settled on paper. The first build sidestepped it entirely (background-swap inert state, undimmed amber glyph) - but per the Revision above, the user chose to match `.casualty-btn:disabled`'s plain opacity fade instead once both were visible side by side, consciously accepting a dimmed amber icon for the multi-model Half-Strength-at-or-below-half-strength case in exchange for one consistent "disabled" look across the whole page. Settled, not open - revisit only if this specific case turns out to be hard to read at the table.
- **Existing tests hardcode old class strings and the span/button distinction.** `LivePlayHalfStrengthAndBattleShockRenderingTests.cs` and (to a lesser extent) `LivePlayCasualtyEndpointTests.cs` assert exact markup. → Mitigation: update them as part of this change's implementation, not as a follow-up.
- **`.claude/design-tokens.md` and `.claude/domain-model-11e.md`'s existing prose about the header glyphs/casualty-reset placement goes stale.** → Mitigation: update `.claude/design-tokens.md`'s relevant sections as part of implementation (per this project's own documentation-maintenance rule in CLAUDE.md).
